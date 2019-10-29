using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {
        public async Task<PingResult> DeviceInitializeAsync(IProgress<DeviceProgress> progress, CancellationToken ct)
        {
            var result = PingResult.OK;

            var n = 0;

            while (n < 3)
            {
                if (ct.IsCancellationRequested) return PingResult.CANCELLED;

                n++;

                progress?.Report(new DeviceProgress() 
                { 
                    PType = ProgressType.TRACE, 
                    PMessage = $"Sending ping request to device:{IpAddress}; Attempt # {n}"
                });
                

                var pingresponse = await PingAsync();

                if (pingresponse == "Success")
                {
                    progress?.Report(new DeviceProgress()
                    {
                        PType = ProgressType.INFO,
                        PMessage = $"Ping Acknowledged!. Device Ip: {IpAddress}"
                    });
                        
                        
                    result = PingResult.OK;
                    break;
                }


                if (n == 3)
                {
                    // already attempted 3 times and it failed every time.
                    result = PingResult.FAILURE;

                    progress?.Report(new DeviceProgress()
                    {
                        PType = ProgressType.ALERT,
                        PMessage = $"Device with Ip Address: {IpAddress} has failed to respond to repeated Ping requests. " +
                                   $"Please check this device and make sure that it is still Online"

                    });
                }
                else
                {
                    await Task.Delay(3000); // give it a 3 sec delay before trying again
                }
            }

            return result;
        }


        /// <summary>
        /// Sends a Ping message to this device every one minute
        /// </summary>
        /// <param name="progress"> object to report progress</param>
        /// <param name="ct"> cancellation token</param>
        /// <returns></returns>
        public async Task StartPingRoutineAsync(IProgress<DeviceProgress> progress, CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(new TimeSpan(0, 0, 1, 0), ct);

                if(!ct.IsCancellationRequested)
                {
                    var response = await PingAsync();

                    if (response == "Success")
                    {
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.INFO,
                            PMessage = $"Ping Acknowleged by Oberon device: {Name}, Location {Location}"
                        });
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.ALERT,
                            PMessage = $"Oberon device with Ip Address {IpAddress} is not responding to the Pings!" +
                                       $"Please make sure this device is still on line"

                        });
                    }
                }
            }
        }

        /// <summary>
        /// Starts the monitor routine that turns this device On /off based on the sunset time
        /// and provided settings
        /// </summary>
        /// <param name="SunsetToday"></param>
        /// <param name="progress"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StartMonitorRoutineAsync(Func<DateTime> SunsetToday, IProgress<DeviceProgress> progress, CancellationToken ct)
        {
            progress.Report(new DeviceProgress()
            {
                PType = ProgressType.INFO,
                PMessage = $"Starting Monitor routine for device: {Name}..."

            });
                
            while (!ct.IsCancellationRequested)
            {
                var sunset = SunsetToday();
                var PMOnTime = sunset - OnTimeOffset;

               await Monitor(PMOnTime, progress, ct);
            }
        }

        private async Task Monitor(DateTime PMOnTime, IProgress<DeviceProgress> progress, CancellationToken ct)
            {
            //var currentTime = DateTime.Now;
            var currentTime = new DateTime(2019, 10, 28, 23, 35, 1);
            var midnight = DateTime.Today;

            if (currentTime < PMOnTime)
            {
                if (AMOnTimeOffest > TimeSpan.Zero)
                {
                    // Morning OnTime is specified for this device:
                    if (currentTime >= midnight && currentTime < midnight + AMOnTimeOffest)
                    {
                       // await SetDeviceOffAsync(progress);

                        // set up the wait for next event:
                        var delaySpan = midnight + AMOnTimeOffest - currentTime;

                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.TRACE,
                            PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                        });

                        Task.Delay(delaySpan, ct).Wait();

                        return;
                    }

                    if (currentTime >= midnight + AMOnTimeOffest && currentTime <= midnight + AMOnTimeOffest + AMOnDuration)
                    {
                        // turn device ON
                       // await SetDeviceOnAsync(progress);

                        // set up the wait for next event:
                        var delaySpan = midnight + AMOnTimeOffest + AMOnDuration - currentTime;

                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.TRACE,
                            PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                        });

                        Task.Delay(delaySpan, ct).Wait();

                        return;
                    }

                    if (currentTime >= midnight + AMOnTimeOffest + AMOnDuration && currentTime < PMOnTime)
                    {
                        // turn device off:
                       // await SetDeviceOffAsync(progress);

                        // set up the wait for next event:
                        var delaySpan = PMOnTime - currentTime;

                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.TRACE,
                            PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                        });

                        Task.Delay(delaySpan, ct).Wait();

                        return;
                    }
                }
                else
                {
                    // No AM On time specified. Keep the device off
                    // turn device off:
                  //  await SetDeviceOffAsync(progress);

                    // set up the wait for next event:
                    var delaySpan = PMOnTime - currentTime;
                    progress?.Report(new DeviceProgress()
                    {
                        PType = ProgressType.TRACE,
                        PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                    });

                    Task.Delay(delaySpan, ct).Wait();

                    return; 
                } 
            }

            if (currentTime >= PMOnTime && currentTime < OffTime)
            {
                // Turn device On:
               // await SetDeviceOnAsync(progress);

                // set up the wait for next event:
                var delaySpan = OffTime - currentTime;

                progress?.Report(new DeviceProgress()
                {
                    PType = ProgressType.TRACE,
                    PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                });

                Task.Delay(delaySpan, ct).Wait();

                return;
            }

            if (currentTime >= OffTime && currentTime < DateTime.Today.AddDays(1))
            {
                // turn device off:
                // await SetDeviceOffAsync(progress);

                // set up the wait for next event:
                var delaySpan = DateTime.Today.AddDays(1) + TimeSpan.FromMinutes(5) - currentTime;

                progress?.Report(new DeviceProgress()
                {
                    PType = ProgressType.TRACE,
                    PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                });

                Task.Delay(delaySpan, ct).Wait();

                return;
            }
        }

        #region Helper Methods
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
                        else if (responseString.EndsWith("OFF", StringComparison.Ordinal))
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
        /// Sets the Device to On state and reports the progress
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        private async Task SetDeviceOnAsync(IProgress<DeviceProgress> progress)
        {
            // Turn device On:
            progress?.Report(new DeviceProgress()
            {
                PType = ProgressType.INFO,
                PMessage = $"Turning {Name} on at {DateTime.Now}... "
            });

            var response = await DeviceOnAsync();

            if ("Success" != response)
            {
                progress?.Report(new DeviceProgress
                {
                    PType = ProgressType.ALERT,
                    PMessage = $"Failure turning device on. Device: {Name}. " +
                               $"Device returned following message {Environment.NewLine} {response}"
                });
            }
        }

        /// <summary>
        /// Sets deivce to Off state and reports the progress
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        private async Task SetDeviceOffAsync(IProgress<DeviceProgress> progress)
        {
            // turn device off:
            progress?.Report(new DeviceProgress()
            {
                PType = ProgressType.INFO,
                PMessage = $"Turning {Name} off at {DateTime.Now}... "
            });

            var response = await DeviceOffAsync();

            if ("Success" != response)
            {
                progress?.Report(new DeviceProgress
                {
                    PType = ProgressType.ALERT,
                    PMessage = $"Failure turning device off. Device: {Name}. " +
                               $"Device returned following message {Environment.NewLine} {response}"
                });
            }
        } 
        #endregion


        #region Oberon API Calls!

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
        #endregion


        ///// <summary>
        ///// This method tells us if it is time to turn keep the Oberon device off.
        ///// (By default, Oberon is ON state). It matches the current time with the 
        ///// following time blocks and tells us if the Oberon should be off
        ///// Block 1 midnight to morning On Time (Oberon OFF)
        ///// Block 2 Morning On Time to Morning Off time (Oberon ON)
        ///// Block 3 Morning Off time to evening On Time (Oberon OFF)
        ///// Block 4 Evening On time to night Off Time (Oberon ON)
        ///// Block 5 Night Off time to midnight (Oberon OFF)
        ///// </summary>
        ///// <param name="sunsetToday"></param>
        //private bool IsOffTimeBlock(DateTime sunsetToday)
        //{   
        //    //var currentTime = new DateTime(2019, 10, 1, 19, 50, 0);
        //    var currentTime = DateTime.Now;

        //    var midnight = DateTime.Today;
        //    var PMOnTime = sunsetToday + OnTimeOffset;

        //    Console.WriteLine($"Current Time: {currentTime.ToShortTimeString()}");

        //    if (AMOnTimeOffest > TimeSpan.Zero) // AM OnTime is specified.
        //    {
        //        if (currentTime >= midnight && currentTime <= midnight + AMOnTimeOffest)
        //        {
        //            Console.WriteLine("We are in block1 - lights off");
        //            return true;
        //        }
        //        else if (currentTime >= midnight + AMOnTimeOffest && currentTime <= midnight + AMOnTimeOffest + AMOnDuration)
        //        {
        //            Console.WriteLine("We are in Block2 - lights on");
        //            return false;
        //        }
        //        else if (currentTime >= midnight + AMOnTimeOffest + AMOnDuration && currentTime <= PMOnTime)
        //        {
        //            Console.WriteLine("We are in Block3 - lights off");
        //            return true;
        //        } 
        //    }
        //    else
        //    {
        //        // No morning OnTime specified. Keep light off
        //        Console.WriteLine("No morning OnTime block found!");
        //    }

        //    // Evening On Times:
        //    if (currentTime >= PMOnTime && currentTime <= OffTime)
        //    {
        //        Console.WriteLine("We are in Block4 - lights On");
        //        return false;
        //    }
        //    else if (currentTime >= OffTime && currentTime <= midnight.AddDays(1))
        //    {
        //        Console.WriteLine("We are in Block5 - lights off");
        //        return true;
        //    }

        //    return true;
        //}
    }
}
