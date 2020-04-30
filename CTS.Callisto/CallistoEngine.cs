using CTS.Juno.Common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;

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

        /// <summary>
        /// Assembles a list of callisto devices based on the configuration settings and
        /// assigns them a Devic key for authourization against the Azure IOT Hub. 
        /// </summary>
        /// <returns></returns>
        private List<CallistoDevice> AssembleCallistoDevices()
        {
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

            return _callistos;
        }

        public void Run(CancellationToken token)
        {
            _logger.LogInformation("Begining Callisto Activities...");

            AssembleCallistoDevices();

            _logger.LogInformation($"{_callistos.Count} Callisto Device(s) found!");

            if(_callistos.Count > 0)
            {
                PrintDeviceList();
            }
            //deviceClient = DeviceClient.Create(ZohalHubUri, 
            //                                   new DeviceAuthenticationWithRegistrySymmetricKey("Callisto00", deviceKey));
            
            //SendDeviceToCloudMessagesAsync();
        }

        private void PrintDeviceList()
        {
            _logger.LogInformation($"Device Name\tDevice ID\tLocation");

            _callistos.ForEach(d =>
            {
                _logger.LogInformation($"{d.Name}\t{d.Id}\t{d.Location}");
            });
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

