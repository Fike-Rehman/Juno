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

        public OberonEngine(ILogger<OberonEngine> logger)
        {
            _oberonDevices = new List<OberonDevice>();
            _logger = logger;
        }

        public void Run(CancellationToken cToken)
        {
            // Begin Oberon Activities

            _logger.LogInformation("Beginning Oberon Activties...");

           
            // See how many Oberon devices we have in the system:
            LoadDevices();

            Task.Run(() => _oberonDevices[0].StartMonitorRoutine(RefreshSunsetTime,
                                                                 new Progress<string>(LogProgress),
                                                                 cToken));



            //cToken.WaitHandle.WaitOne(15 * 1000);


            // Initialize the devices found:
            //InitDevicesAsync(cToken).Wait();

            //// Start the Task to run the Ping routines for each device:
            //_logger.LogInformation("Device initialization Completed!");
            //_logger.LogInformation($"{_oberonDevices.Count} active Oberon devices(s) detected during initialization!");

            //try
            //{
            //    var oberonTasks = new List<Task>();

            //    // Launch ping routines for all the initialized devices:
            //    _oberonDevices.ForEach(d =>
            //    {
            //        if (cToken.IsCancellationRequested) return;

            //        var pt = Task.Run(() => d.StartPingRoutine(new Progress<string>(LogProgress), cToken));

            //        _logger.LogInformation($"Ping routine for Oberon device :{d.Name} started!");

            //        oberonTasks.Add(pt);
            //    });

            //    Task.Delay(1000, cToken);

            //    // Launch Monitor routines for all the initialized devices:
            //    _oberonDevices.ForEach(d =>
            //    {
            //        if (cToken.IsCancellationRequested) return;

            //        var mt = Task.Run(() => d.StartMonitorRoutine(RefreshSunsetTime,
            //                                                      new Progress<string>(LogProgress),
            //                                                      cToken));

            //        _logger.LogInformation($"Monitor routine for Oberon device :{d.Name} started!");

            //        oberonTasks.Add(mt);
            //    });

            //    Task.WaitAll(oberonTasks.ToArray());
            //}
            //catch (Exception x)
            //{
            //    _logger.LogError("Exception while running Oberon Tasks!");
            //    _logger.LogError(x.Message);
            //    _logger.LogError(x.InnerException.Message);
            //}
        }

        public DateTime RefreshSunsetTime()
        {
            // Get the sunrise/sunset times 
            SolarTimes.GetSolarTimes(out DateTime _sunriseToday,
                                     out DateTime _sunsetToday);

            _logger.LogInformation($"Today's Sunset time: {_sunsetToday}");
            return _sunsetToday;
        }

        /// <summary>
        /// logs the messages reported by the devices
        /// </summary>
        /// <param name="progressString"></param>
        private void LogProgress(string progressString)
        {
            _logger.LogInformation(progressString);
        }

        /// <summary>
        /// loads the devices from the given json file:
        /// </summary>
        private void LoadDevices()
        {
            try
            {
               // string path = Directory.GetCurrentDirectory();

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

        /// <summary>
        /// initializes the devices that are defined in the JSON device file
        /// by sending a ping message to each of those devices. If the ping
        /// to a devics fails after repeated attempts, that device is removed
        /// from the list. 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
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
