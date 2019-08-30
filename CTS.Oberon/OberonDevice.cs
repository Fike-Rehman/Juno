
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {

        //private static readonly log4net.ILog _logger =
        //         log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Logger<OberonDevice> _logger;

       // private Timer _pingTimer;

       // private Timer _deviceMonitorTimer;

        
        public async Task StartPingRoutine(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(new TimeSpan(0, 0, 1, 0));

                if(!ct.IsCancellationRequested)
                {
                    var response = await PingAsync(IpAddress);

                    if (response == "Success")
                    {
                        _logger.LogDebug($"Ping Acknowleged! Device Ip: {IpAddress}");
                       
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        _logger.LogWarning($"Device with Ip Address {IpAddress} is not responding to the Pings!");
                        _logger.LogWarning($"Please make sure this device is still on line");
                    }

                }
            }
        }

        public void StartMonitorRoutine()
        {
            var monitorInterval = new TimeSpan(0, 0, 30); // every 30 secs

           // _deviceMonitorTimer = new Timer(OnMonitorTimer, null, monitorInterval, Timeout.InfiniteTimeSpan);
        }


        public async Task<PingResult> DevicePingAsync(string deviceIp, CancellationToken ct)
        {
            var result = PingResult.OK;

            var n = 0;

            while (n < 3)
            {
                if (ct.IsCancellationRequested) return PingResult.CANCELLED;

                n++;

                _logger.LogDebug($"Sending ping request to device:{IpAddress}; Attempt # {n}");

                var pingresponse = await PingAsync(deviceIp);

                if (pingresponse == "Success")
                {
                    _logger.LogDebug($"Ping Acknowledged!. Device Ip: {IpAddress}");
                    result = PingResult.OK;
                    break;
                }


                if (n == 3)
                {
                    // already attempted 3 times and it failed every time.
                    result = PingResult.FAILURE;
                    _logger.LogError($"Device with Ip Address: {deviceIp} has failed to respond to repeated Ping requests");
                    _logger.LogError("Please check this device and make sure that it is still On line");
                }
                else
                {
                    await Task.Delay(3000); // give it a 3 sec delay before trying again
                }
            }

            return result;
        }

        private async Task<string> PingAsync(string deviceIp)
        {
            var pingResponse = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{deviceIp}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var response = await client.GetAsync("/ping");


                    if (response.IsSuccessStatusCode)
                    {
                        pingResponse = "Success";
                    }

                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    pingResponse = x.Message;
                }

                return pingResponse;
            }
        }


        //private async void OnPingTimer(object device)
        //{
        //    // send a ping asynchronously and reset the timer

        //    var response = await PingAsync(IpAddress);

        //    if (response == "Success")
        //    {
        //        _logger.Debug($"Ping Acknowleged! Device Ip: {IpAddress}");
        //        var pingInterval = new TimeSpan(0, 0, 1, 0); // 1 minute
        //        _pingTimer.Change(pingInterval, Timeout.InfiniteTimeSpan);
        //    }
        //    else
        //    {
        //        // Device has failed to respond to the Ping request
        //        _logger.Warn($"Device with Ip Address {IpAddress} is not responding to the Pings!");
        //        _logger.Warn($"Please make sure this device is still on line");
        //    }
        //}

        //private async void OnMonitorTimer(object device)
        //{
        //    // TODO: Implement device monitoring here.
        //    await Task.Delay(3000);
        //}
    }
}
