using CTS.Common.Utilities;
using CTS.Juno.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public class OberonEngine : IDeviceEngine
    {
        private readonly ILogger<OberonEngine> _logger;

        private readonly IAppSettings _appSettings;

        private readonly List<OberonDevice> _oberonDevices;

        // last time solar data was updated:
        private DateTime _lastSolarUpdate;

        // Today's sunset time:
        private DateTime _sunsetToday;

        public OberonEngine(ILogger<OberonEngine> logger, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger = logger;
            _appSettings = appSettings.Value;

            _oberonDevices = new List<OberonDevice>();
           
            _lastSolarUpdate = DateTime.MinValue;
            _sunsetToday = DateTime.MinValue;
        }

        public void Run(CancellationToken cToken)
        {
            // Begin Oberon Activities
            _logger.LogInformation("Beginning Oberon Activities...");

            // Build Oberon device list:
            try
            {
                foreach (var device in _appSettings.Devicelist.JunoDevices)
                {
                    if (device.Id.StartsWith("Oberon"))
                    {
                        var settings = new OberonSettings()
                        {
                            Id = device.Id,
                            Name = device.Name,
                            SerialNumber = device.SerialNumber,
                            ProvisionDate = device.ProvisionDate,
                            IpAddress = device.IpAddress,
                            Location = device.Location,
                            TimeSettings = device.Settings
                        };

                        _oberonDevices.Add(new OberonDevice(settings));
                    }
                }

                // Initialize the devices found:
                // We must wait for this to complete before proceeding
                InitDevicesAsync(cToken).Wait();

                _logger.LogInformation("Device initialization Completed!");
                _logger.LogInformation($"{_oberonDevices.Count} active Oberon devices(s) detected during initialization!");

               
                var oberonTasks = new List<Task>();

                // Launch ping routines for all the initialized devices:
                _oberonDevices.ForEach(d =>
                {
                    if (cToken.IsCancellationRequested) return;

                    var pt = Task.Run(() => d.StartPingRoutineAsync(new Progress<DeviceProgress>(LogProgress), cToken));

                    _logger.LogInformation($"Ping routine for Oberon device :{d.Id} started!");

                    oberonTasks.Add(pt);

                    Task.Delay(2000, cToken).Wait();
                });

                // Launch Monitor routines for all the initialized devices:
                _oberonDevices.ForEach(d =>
                {
                    if (cToken.IsCancellationRequested) return;

                    var mt = Task.Run(() => d.StartMonitorRoutineAsync(RefreshSunsetTime,
                                                                        new Progress<DeviceProgress>(LogProgress),
                                                                        cToken));

                    _logger.LogInformation($"Monitor routine for Oberon device :{d.Id} started!");

                    oberonTasks.Add(mt);

                    Task.Delay(2000, cToken).Wait();
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

        
        private async Task InitDevicesAsync(CancellationToken ct)
        {
            if (_oberonDevices.Count > 0)
            {
                for (int i = _oberonDevices.Count - 1; i >= 0; i--)
                {
                    if (ct.IsCancellationRequested) break;

                    var device = _oberonDevices[i];

                    _logger.LogDebug("Beginning device initialization....");

                    var result = await device.DeviceInitializeAsync(new Progress<DeviceProgress>(LogProgress), ct);

                    if (result == PingResult.FAILURE)
                    {
                        _logger.LogWarning($"Removing device with IP Address:{device.Id} from device list because it doesn't appear to be on line");

                        _oberonDevices.Remove(device);
                    }
                    else if (result == PingResult.CANCELLED)
                    {
                        _logger.LogWarning("Device initialization canceled upon user request!");
                    }
                    else
                    {
                        _logger.LogDebug($"Device Ping Successful! Device Id:{device.Id}");
                    }
                }
            }
        }
    }
}
