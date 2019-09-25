using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {
        public async Task StartPingRoutine(IProgress<string> progress, CancellationToken ct)
        {
            
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(new TimeSpan(0, 0, 1, 0));

                if(!ct.IsCancellationRequested)
                {
                    var response = await PingAsync(IpAddress);

                    if (response == "Success")
                    {
                        progress?.Report($"Ping Acknowleged! Device Ip: {IpAddress}");
                       
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        progress?.Report($"Device with Ip Address {IpAddress} is not responding to the Pings!");
                        progress?.Report($"Please make sure this device is still on line");
                    }
                }
            }
        }

        public async Task StartMonitorRoutine(DateTime SunsetToday, IProgress<string> progress, CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(new TimeSpan(0, 0, 0, 30)); // check every 30 secs

                

            }
        }


        public async Task<PingResult> DevicePingAsync(string deviceIp, IProgress<string> progress, CancellationToken ct)
        {
            var result = PingResult.OK;

            var n = 0;

            while (n < 3)
            {
                if (ct.IsCancellationRequested) return PingResult.CANCELLED;

                n++;

                progress?.Report($"Sending ping request to device:{IpAddress}; Attempt # {n}");

                var pingresponse = await PingAsync(deviceIp);

                if (pingresponse == "Success")
                {
                    progress?.Report($"Ping Acknowledged!. Device Ip: {IpAddress}");
                    result = PingResult.OK;
                    break;
                }


                if (n == 3)
                {
                    // already attempted 3 times and it failed every time.
                    result = PingResult.FAILURE;
                    progress?.Report($"Device with Ip Address: {deviceIp} has failed to respond to repeated Ping requests");
                    progress?.Report("Please check this device and make sure that it is still On line");
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

        public void IsOffTimeBlock(DateTime sunsetToday)
        {
            //bool result = true;

            // Check to see if we are in off time block

            var currentTime = new DateTime(2019, 9, 24, 10, 35, 0);
            var midnight = DateTime.Today;

            Console.WriteLine($"Current Time: {currentTime.ToShortTimeString()}");

            if(currentTime > midnight && currentTime <= midnight + AMOnTimeOffest)
            {
                Console.WriteLine("We are in block1 - lights off");
            }
            else if(currentTime >= midnight + AMOnTimeOffest && currentTime <= midnight + AMOnTimeOffest + AMOnDuration)
            {
                Console.WriteLine("We are in Block2 - lights on");
            }
            else if (currentTime >= midnight + AMOnTimeOffest + AMOnDuration  && currentTime <= sunsetToday)
            {
                Console.WriteLine("We are in Block3 - lights off");
            }
            else if (currentTime >= sunsetToday && currentTime <= OffTime)
            {
                Console.WriteLine("We are in Block4 - lights On");
            }
            else if (currentTime >= OffTime && currentTime <= midnight.AddDays(1))
            {
                Console.WriteLine("We are in Block5 - lights off");
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
