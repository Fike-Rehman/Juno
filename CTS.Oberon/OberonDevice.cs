using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {
        /// <summary>
        /// Sends a Ping message to this device every one minute
        /// </summary>
        /// <param name="progress"> object to report progress</param>
        /// <param name="ct"> cancellation token</param>
        /// <returns></returns>
        public async Task StartPingRoutine(IProgress<string> progress, CancellationToken ct)
        {
            
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(new TimeSpan(0, 0, 1, 0), ct);

                if(!ct.IsCancellationRequested)
                {
                    var response = await PingAsync();

                    if (response == "Success")
                    {
                        progress?.Report($"Ping Acknowleged by Oberon device: {Name}, Location {Location}");
                       
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        progress?.Report($"Oberon device with Ip Address {IpAddress} is not responding to the Pings!");
                        progress?.Report($"Please make sure this device is still on line");
                    }
                }
            }
        }

        
        public async Task StartMonitorRoutine(Func<DateTime> SunsetToday, IProgress<string> progress, CancellationToken ct)
        {
            progress.Report($"Starting Monitor routine for device: {Name}...");


            while (!ct.IsCancellationRequested)
            {
                var sunset = SunsetToday();
                var PMOnTime = sunset - OnTimeOffset;

               await Monitor(PMOnTime, progress, ct);
            }
        }

        private async Task Monitor(DateTime PMOnTime, IProgress<string> progress, CancellationToken ct)
        {
            var currentTime = DateTime.Now;
            var midnight = DateTime.Today;


            if (currentTime >= midnight && currentTime < midnight + AMOnTimeOffest)
            {
                progress.Report($"Turning {Name} off at {DateTime.Now}... ");

                var response = await DeviceOffAsync();

                if("Success" != response)
                {
                    progress?.Report($"Error turning device off. Device: {Name}");
                    progress?.Report(response);
                }

                var delaySpan = midnight + AMOnTimeOffest - currentTime;
                Console.WriteLine($"wait started at : {DateTime.Now.ToShortTimeString()}");
                Console.WriteLine($"delay span: {delaySpan}");

                Task.Delay(delaySpan, ct).Wait();

                
                
                Console.WriteLine("exiting...");
                return;
            }

            if (currentTime >= midnight + AMOnTimeOffest && currentTime <= midnight + AMOnTimeOffest + AMOnDuration)
            {
                
                Console.WriteLine("In Block2: turning lights on");
                // turn lights ON

                var delayspan = midnight + AMOnTimeOffest + AMOnDuration - currentTime;

                Console.WriteLine($"wait started at : {DateTime.Now.ToShortTimeString()}");
                Console.WriteLine($"delay span: {delayspan}");

                
                Task.Delay(delayspan, ct);
                Console.WriteLine("exiting...");
                return;
            }

            if (currentTime >= midnight + AMOnTimeOffest + AMOnDuration && currentTime < PMOnTime)
            {
                Console.WriteLine("In Block3: turning lights off");
                // turn lights OFF

                var delayspan = PMOnTime - currentTime;
                Console.WriteLine($"wait started at : {DateTime.Now.ToShortTimeString()}");
                Console.WriteLine($"delay span: {delayspan}");
                Task.Delay(delayspan, ct);
                Console.WriteLine("exiting...");
                return;
            }

            if (currentTime >= PMOnTime && currentTime < OffTime)
            {
                Console.WriteLine("In Block4: turning lights On");
                // turn lights ON

                var delayspan = OffTime - currentTime;
                Console.WriteLine($"wait started at : {DateTime.Now.ToShortTimeString()}");
                Console.WriteLine($"delay span: {delayspan}");
                Task.Delay(delayspan, ct);
                Console.WriteLine("exiting...");
                return;
            }

            if (currentTime >= OffTime && currentTime < DateTime.Today.AddDays(1))
            {
                Console.WriteLine("In Block5: turning lights off");
                // turn lights OFF

                var delayspan = DateTime.Today.AddDays(1) - currentTime;
                Console.WriteLine($"wait started at : {DateTime.Now.ToShortTimeString()}");
                Console.WriteLine($"delay span: {delayspan}");
                Task.Delay(delayspan, ct);
                Console.WriteLine("exiting...");
                return;
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

                var pingresponse = await PingAsync();

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

        private async Task<string> PingAsync()
        {
            var pingResponse = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{IpAddress}");
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

        private async Task<string> DeviceOffAsync()
        {
            var offResponse = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{IpAddress}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var response = await client.GetAsync("/off");


                    if (response.IsSuccessStatusCode)
                    {
                        offResponse = "Success";
                    }

                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    offResponse = x.Message;
                }

                return offResponse;
            }
        }

        private async Task<string> DeviceOnAsync()
        {
            var onResponse = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{IpAddress}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var response = await client.GetAsync("/on");


                    if (response.IsSuccessStatusCode)
                    {
                        onResponse = "Success";
                    }

                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    onResponse = x.Message;
                }

                return onResponse;
            }
        }

        public async Task<string> GetDeviceStatusAsync()
        {
            var dStatus = "UNKNOWN";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{IpAddress}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var response = await client.GetAsync("/status");

                    if (response.IsSuccessStatusCode)
                    {
                        //parse the message content
                        var responseString = await response.Content.ReadAsStringAsync();

                        if (responseString.EndsWith("ON", StringComparison.Ordinal))
                        {
                            dStatus = "ON";
                        }
                        else if(responseString.EndsWith("OFF", StringComparison.Ordinal))
                        {
                            dStatus = "OFF";
                        }
                    }
                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    dStatus = ": " + x.Message;
                }

                return dStatus;
            }
        }


        /// <summary>
        /// This method tells us if it is time to turn keep the Oberon device off.
        /// (By default, Oberon is ON state). It matches the current time with the 
        /// following time blocks and tells us if the Oberon should be off
        /// Block 1 midnight to morning On Time (Oberon OFF)
        /// Block 2 Morning On Time to Morning Off time (Oberon ON)
        /// Block 3 Morning Off time to evening On Time (Oberon OFF)
        /// Block 4 Evening On time to night Off Time (Oberon ON)
        /// Block 5 Night Off time to midnight (Oberon OFF)
        /// </summary>
        /// <param name="sunsetToday"></param>
        private bool IsOffTimeBlock(DateTime sunsetToday)
        {   
            //var currentTime = new DateTime(2019, 10, 1, 19, 50, 0);
            var currentTime = DateTime.Now;

            var midnight = DateTime.Today;
            var PMOnTime = sunsetToday + OnTimeOffset;

            Console.WriteLine($"Current Time: {currentTime.ToShortTimeString()}");

            if (AMOnTimeOffest > TimeSpan.Zero) // AM OnTime is specified.
            {
                if (currentTime >= midnight && currentTime <= midnight + AMOnTimeOffest)
                {
                    Console.WriteLine("We are in block1 - lights off");
                    return true;
                }
                else if (currentTime >= midnight + AMOnTimeOffest && currentTime <= midnight + AMOnTimeOffest + AMOnDuration)
                {
                    Console.WriteLine("We are in Block2 - lights on");
                    return false;
                }
                else if (currentTime >= midnight + AMOnTimeOffest + AMOnDuration && currentTime <= PMOnTime)
                {
                    Console.WriteLine("We are in Block3 - lights off");
                    return true;
                } 
            }
            else
            {
                // No morning OnTime specified. Keep light off
                Console.WriteLine("No morning OnTime block found!");
            }

            // Evening On Times:
            if (currentTime >= PMOnTime && currentTime <= OffTime)
            {
                Console.WriteLine("We are in Block4 - lights On");
                return false;
            }
            else if (currentTime >= OffTime && currentTime <= midnight.AddDays(1))
            {
                Console.WriteLine("We are in Block5 - lights off");
                return true;
            }

            return true;
        }
    }
}
