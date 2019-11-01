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

        // last time solar data was updated:
        private DateTime _lastSolarUpdate;

        // Today's sunset time:
        private DateTime _sunsetToday;

        public OberonEngine(ILogger<OberonEngine> logger)
        {
            _oberonDevices = new List<OberonDevice>();
            _logger = logger;

            _lastSolarUpdate = DateTime.MinValue;
            _sunsetToday = DateTime.MinValue;
        }

        public void Run(CancellationToken cToken)
        {
            // Begin Oberon Activities

            _logger.LogInformation("Beginning Oberon Activties...");

            // See how many Oberon devices we have in the system:
            LoadDevices();

            //Task.Run(() => _oberonDevices[0].StartMonitorRoutineAsync(RefreshSunsetTime,
            //                                                     new Progress<DeviceProgress>(LogProgress),
            //                                                     cToken));


            // Initialize the devices found:
            // We must wait for this to complete before proceeding
            InitDevicesAsync(cToken).Wait();

            _logger.LogInformation("Device initialization Completed!");
            _logger.LogInformation($"{_oberonDevices.Count} active Oberon devices(s) detected during initialization!");

            try
            {
                var oberonTasks = new List<Task>();

                // Launch ping routines for all the initialized devices:
                _oberonDevices.ForEach(d =>
                {
                    if (cToken.IsCancellationRequested) return;

                    var pt = Task.Run(() => d.StartPingRoutineAsync(new Progress<DeviceProgress>(LogProgress), cToken));

                    _logger.LogInformation($"Ping routine for Oberon device :{d.Name} started!");

                    oberonTasks.Add(pt);
                });

                Thread.Sleep(1000);

                // Launch Monitor routines for all the initialized devices:
                _oberonDevices.ForEach(d =>
                {
                    if (cToken.IsCancellationRequested) return;

                    var mt = Task.Run(() => d.StartMonitorRoutineAsync(RefreshSunsetTime,
                                                                       new Progress<DeviceProgress>(LogProgress),
                                                                       cToken));

                    //_logger.LogInformation($"Monitor routine for Oberon device :{d.Name} started!");

                    oberonTasks.Add(mt);
                });

                Task.WaitAll(oberonTasks.ToArray());
            }
            catch (Exception x)
            {
                _logger.LogError("Exception while running Oberon Tasks!");
                _logger.LogError(x.Message);
                _logger.LogError(x.InnerException.Message);
            }
        }

        /// <summary>
        /// refreshes solar data if it is older than 24 hrs and returns
        /// today's sunset time
        /// </summary>
        /// <returns></returns>
        public DateTime RefreshSunsetTime()
        {
            if(_lastSolarUpdate.Date != DateTime.Now.Date)
            {
                // Get the sunrise/sunset times 
                SolarTimes.GetSolarTimes(out DateTime sunriseToday,
                                         out DateTime sunsetToday);

                _sunsetToday = sunsetToday;
                _lastSolarUpdate = DateTime.Now;

                _logger.LogInformation($"Solar Data updated at: {_lastSolarUpdate}");
                _logger.LogInformation($"Today's Sunset time: {_sunsetToday}");
            }

            return _sunsetToday;
        }

        /// <summary>
        /// logs the messages reported by the devices
        /// </summary>
        /// <param name="progressString"></param>
        private void LogProgress(DeviceProgress progressReport)
        {
            if(progressReport.PType == ProgressType.TRACE)
            {
               // Console.WriteLine(progressReport.PMessage);
                _logger.LogDebug(progressReport.PMessage);
            }
            else if(progressReport.PType == ProgressType.INFO)
            {
                _logger.LogInformation(progressReport.PMessage);
            }
            else if(progressReport.PType == ProgressType.ALERT)
            {
                _logger.LogError(progressReport.PMessage);
            }
        }

        /// <summary>
        /// loads the devices from the given json file:
        /// </summary>
        private void LoadDevices()
        {
            try
            {
                using (StreamReader file = File.OpenText("OberonDevices.json"))
                {
                    // var serialize = new JsonSerializer();
                    string jsonString = file.ReadToEnd();
                    _oberonDevices = JsonConvert.DeserializeObject<List<OberonDevice>>(jsonString);

                    _logger.LogDebug($"Found {_oberonDevices.Count} Oberon device(s) defined in the system!");
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

                    _logger.LogInformation("Beginning device initialization....");

                    var result = await device.DeviceInitializeAsync(new Progress<DeviceProgress>(LogProgress), ct);

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
                        _logger.LogDebug($"Device Ping Successful! Ip Address:{device.IpAddress}");
                    }
                }
            }
        }
    }
}
