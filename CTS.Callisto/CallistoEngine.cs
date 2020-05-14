using CTS.Juno.Common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Callisto
{

    //public class SimulatedCallistoDevice
    //{
    //    public int TempF { get; set; }

    //    public int Humidity { get; set; }
    //}


    public class CallistoEngine : IDeviceEngine
    {
        private readonly ILogger<CallistoEngine> _logger;

        private readonly IAppSettings _appSettings;

        private readonly ISecureSettings _secureSettings;

        private string ZohalHubUri;

        private List<CallistoDevice> _callistos = null;

       // const string deviceKey = "wXxFEeYqmA90pIqugGgi93HCruEKIont/7KZV44WqaM=";

        static DeviceClient deviceClient;

        public CallistoEngine(ILogger<CallistoEngine> logger,  IOptions<AppSettings> appSettings, ISecureSettings secureSettings)
        {
            _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger = logger;
            _appSettings = appSettings.Value;
            _secureSettings = secureSettings;

            _callistos = new List<CallistoDevice>();
        }

        
        public void Run(CancellationToken cToken)
        {
            _logger.LogInformation("Begining Callisto Activities...");

            // read callisto devices configuration and build the device list

            foreach (var device in _appSettings.Devicelist.JunoDevices)
            {
                if (device.Id.StartsWith("Callisto"))
                {
                    _callistos.Add(new CallistoDevice
                    {
                        Id = device.Id,
                        Name = device.Name,
                        SerialNumber = device.SerialNumber,
                        ProvisionDate = device.ProvisionDate,
                        IpAddress = device.IpAddress,
                        Location = device.Location,
                        DeviceKey = _secureSettings.GetDeviceKey(device.Id),
                    });
                }  
            }

            _logger.LogInformation($"{_callistos.Count} Callisto Device(s) found in the configuration!");

            if(_callistos.Count > 0)
            {
                // Initialize each device defined in the config by sending a ping
                // to see if it is Online. We must wait for this to complete before proceeding

                InitDevicesAsync(cToken).Wait();

                // print a list of online devices:
                _logger.LogInformation($"Found {_callistos.Count} callistos devices online:");

                _logger.LogInformation($"Device ID\tLocation\tIp Address");

                _callistos.ForEach(d =>
                {
                    _logger.LogInformation($"{d.Id}\t{d.Location}\t{d.IpAddress}");
                });

                try
                {
                    var callistoTasks = new List<Task>();

                    // Launch ping routines for all the initialized devices:
                    _callistos.ForEach(callisto =>
                    {
                        if (cToken.IsCancellationRequested) return;

                        var pt = Task.Run(() => callisto.StartPingRoutineAsync(new Progress<DeviceProgress>(LogProgress), cToken));

                        _logger.LogInformation($"Ping routine for Callisto device :{callisto.Id} started!");

                        callistoTasks.Add(pt);
                    });

                    cToken.WaitHandle.WaitOne(1000);

                    // Launch Monitor Routines for all initialized devices:
                    _callistos.ForEach(callisto =>
                    {
                        if (cToken.IsCancellationRequested) return;

                        var mt = Task.Run(() => callisto.StartMonitorRoutineAsync(new Progress<DeviceProgress>(LogProgress), cToken));

                        _logger.LogInformation($"Monitor routine for Callisto device :{callisto.Id} started!");

                        callistoTasks.Add(mt);
                    });
                }
                catch(Exception x)
                {
                    _logger.LogError("Exception while running Oberon Tasks!");
                    _logger.LogError(x.Message);
                    _logger.LogError(x.InnerException.Message);
                }
            }
            //deviceClient = DeviceClient.Create(ZohalHubUri, 
            //                                   new DeviceAuthenticationWithRegistrySymmetricKey("Callisto00", deviceKey));
            
            //SendDeviceToCloudMessagesAsync();
        }

        /// <summary>
        /// initializes the devices that are defined in the configuration
        /// by sending a ping message to each of those devices. If the ping
        /// to a devics fails after repeated attempts, that device is removed
        /// from the list (assuming that we have configured a bad device or the 
        /// configuration is wrong) 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task InitDevicesAsync(CancellationToken ct)
        {
            if(_callistos.Count > 0)
            {
                for (int i = _callistos.Count - 1; i >= 0; i--)
                {
                    if (ct.IsCancellationRequested) break;

                    var device = _callistos[i];

                    _logger.LogInformation("Beginning device initialization....");

                    var result = await device.DeviceInitializeAsync(new Progress<DeviceProgress>(LogProgress), ct);

                    if (result == PingResult.FAILURE)
                    {
                        _logger.LogWarning($"Removing device with IP Address:{device.IpAddress} from device list because it doesn't appear to be online");

                        _callistos.Remove(device);
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

        /// <summary>
        /// logs the messages reported by the devices
        /// </summary>
        /// <param name="progressString"></param>
        private void LogProgress(DeviceProgress progressReport)
        {
            if (progressReport.PType == ProgressType.TRACE)
            {
                 Console.WriteLine(progressReport.PMessage);
                _logger.LogDebug(progressReport.PMessage);
            }
            else if (progressReport.PType == ProgressType.INFO)
            {
                _logger.LogInformation(progressReport.PMessage);
            }
            else if (progressReport.PType == ProgressType.ALERT)
            {
                _logger.LogError(progressReport.PMessage);
            }
        }


        /// <summary>
        /// Here load Callisto devices from device store and match them up with 
        /// their Azure IOT hub device keys
        /// </summary>


        //private async void SendDeviceToCloudMessagesAsync()
        //{
        //    var rnd = new Random();

        //    while (true)
        //    {
        //        var callistoDevice = new SimulatedCallistoDevice
        //        {
        //            TempF = rnd.Next(-40, 40),
        //            Humidity = rnd.Next(10, 90)
        //        };

        //        var telemetryDataPoint = new
        //        {
        //            deviceId = "Callisto00",
        //            callistodeviceData = callistoDevice
        //        };
        //        var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
        //        var message = new Message(Encoding.ASCII.GetBytes(messageString));

        //        await deviceClient.SendEventAsync(message);
        //        _logger.LogInformation($"Callisto message sent at: {DateTime.Now}, MessageString: {messageString}");

        //        Task.Delay(1000).Wait();
        //    }
        //}
    }
}

