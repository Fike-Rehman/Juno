using CTS.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public class OberonEngine
    {
        private static readonly log4net.ILog _logger =
                 log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<OberonDevice> _oberonDevices;

        private DateTime _sunriseToday;
        private DateTime _sunsetToday;



        public OberonEngine()
        {
            _oberonDevices = new List<OberonDevice>();
        }

        public void Run(CancellationToken cToken)
        {
            // Begin Oberon Activities

            // Get the sunrise/sunset times 
            SolarTimes.GetSolarTimes(out _sunriseToday, out _sunsetToday);

            // See how many Oberon devices we have in the system:
            LoadDevices();

            // Initialize the devices found:
            InitDevicesAsync(cToken).Wait();

            // Start the Task to run the Ping routines for each device:

            _logger.Debug("Device initialization Completed!");
            _logger.Debug($"{_oberonDevices.Count} active Oberon devices(s) detected during initialization!");

            var pingTasks = new List<Task>();

            _oberonDevices.ForEach(d =>
            {
                var pt = Task.Run(() => d.StartPingRoutine(), cToken);
                pingTasks.Add(pt);

                if (cToken.IsCancellationRequested) return;
            });
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

                    _logger.Info($"Found {_oberonDevices.Count} Oberon devices defined in the system!");
                }
            }
            catch (Exception x)
            {

                _logger.Error($"Error while reading Oberon Devices file: {x.Message}");
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

                    _logger.Debug($"Pinging device {device.IpAddress}....");

                    var result = await device.DevicePingAsync(device.IpAddress, ct);

                    if (result == PingResult.FAILURE)
                    {
                        _logger.Warn($"Removing device with IP Address:{device.IpAddress} from device list because it doesn't appear to be on line");

                        _oberonDevices.Remove(device);
                    }
                    else if (result == PingResult.CANCELLED)
                    {
                        _logger.Debug("Device initialization canceled upon user request!");
                    }
                    else
                    {
                        _logger.Debug($"Device Ping Successful! Ip Address:{device.IpAddress}");
                    }
                }
            }
        }
    }
}
