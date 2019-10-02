using CTS.Common.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace CTS.Oberon
{
    public class OberonEngine : IDeviceEngine
    {
        private readonly ILogger<OberonEngine> _logger;

        private List<OberonDevice> _oberonDevices;

        private DateTime _sunriseToday;
        private DateTime _sunsetToday;

        public OberonEngine(ILogger<OberonEngine> logger)
        {
            _oberonDevices = new List<OberonDevice>();
            _logger = logger;
        }

        public void Run(CancellationToken cToken)
        {
            // Begin Oberon Activities

            _logger.LogInformation("Beginning Oberon Activties...");
            
            // Get the sunrise/sunset times 
            SolarTimes.GetSolarTimes(out _sunriseToday, out _sunsetToday);

            // See how many Oberon devices we have in the system:
            LoadDevices();

            // _oberonDevices[1].IsOffTimeBlock(_sunsetToday);
            //_oberonDevices[0].GetDeviceStatusAsync().Wait();

            // Initialize the devices found:
            InitDevicesAsync(cToken).Wait();

            // Start the Task to run the Ping routines for each device:
            _logger.LogInformation("Device initialization Completed!");
            _logger.LogInformation($"{_oberonDevices.Count} active Oberon devices(s) detected during initialization!");

            var pingTasks = new List<Task>();

            _oberonDevices.ForEach(d =>
            {
                if (cToken.IsCancellationRequested) return;

                // Create the progress object and start the ping routine:
                var progress = new Progress<string>(LogProgress);

                var pt = Task.Run(() => d.StartPingRoutine(progress, cToken));

                
                pingTasks.Add(pt); 
            });
        }

        /// <summary>
        /// logs the messages reported by the devices
        /// </summary>
        /// <param name="progressString"></param>
        private void LogProgress(string progressString)
        {
            _logger.LogInformation(progressString);
        }


        private void LoadDevices()
        {
            try
            {
                using (StreamReader file = File.OpenText("OberonDevices.json"))
                {
                    // var serialize = new JsonSerializer();
                    string jsonString = file.ReadToEnd();
                    _oberonDevices = JsonConvert.DeserializeObject<List<OberonDevice>>(jsonString);

                    _logger.LogInformation($"Found {_oberonDevices.Count} Oberon devices defined in the system!");
                }
            }
            catch (Exception x)
            {

                _logger.LogError($"Error while reading Oberon Devices file: {x.Message}");
            }
        }


        private async Task InitDevicesAsync(CancellationToken ct)
        {
            if (_oberonDevices.Count > 0)
            {
                for (int i = _oberonDevices.Count - 1; i >= 0; i--)
                {
                    if (ct.IsCancellationRequested) break;

                    var device = _oberonDevices[i];

                    _logger.LogDebug($"Pinging device {device.IpAddress}....");

                    var progress = new Progress<string>(msg => _logger.LogDebug(msg));

                    var result = await device.DevicePingAsync(device.IpAddress, progress, ct);

                    if (result == PingResult.FAILURE)
                    {
                        _logger.LogWarning($"Removing device with IP Address:{device.IpAddress} from device list because it doesn't appear to be on line");

                        _oberonDevices.Remove(device);
                    }
                    else if (result == PingResult.CANCELLED)
                    {
                        _logger.LogWarning("Device initialization canceled upon user request!");
                    }
                    else
                    {
                        _logger.LogWarning($"Device Ping Successful! Ip Address:{device.IpAddress}");
                    }
                }
            }
        }
    }
}
