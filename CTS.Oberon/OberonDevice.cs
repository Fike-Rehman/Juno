using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CTS.Juno.Common;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {
        public string Id { get; private set; }

        private readonly OberonSettings _settings;

        public OberonDevice(OberonSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            Id = _settings.Id;


        }
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
                    PMessage = $"Sending ping request to device:{_settings.IpAddress}; Attempt # {n}"
                });
               
                var pingresponse = _settings.Id.EndsWith("00") ? await SimPingAsync()
                                                : await PingAsync();

                if (pingresponse == "Success")
                {
                    progress?.Report(new DeviceProgress()
                    {
                        PType = ProgressType.INFO,
                        PMessage = $"Ping Acknowledged!. Device Ip: {_settings.IpAddress}"
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
                        PMessage = $"Device with Ip Address: {_settings.IpAddress} has failed to respond to repeated Ping requests. " +
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
                   // if this is a simulated device, send a simulated Ping, otherwise send a real ping
                   var response = _settings.Id.EndsWith("00") ? await SimPingAsync() 
                                             : await PingAsync();

                    if (response == "Success")
                    {
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.INFO,
                            PMessage = $"Ping Acknowledged by Oberon device: {_settings.Id}, Location {_settings.Location}"
                        });
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.ALERT,
                            PMessage = $"Oberon device with Ip Address {_settings.IpAddress} is not responding to the Pings!" +
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
                PMessage = $"Starting Monitor routine for device: {_settings.Id}..."

            });

            while (!ct.IsCancellationRequested)
            {
                var sunset = SunsetToday();
                var OnTimeOffset = TimeSpan.Parse(_settings.TimeSettings[0].OnTimeOffset);
                var PMOnTime = sunset - OnTimeOffset;


                await Monitor(PMOnTime, progress, ct);
            }
        }

        private async Task Monitor(DateTime PMOnTime, IProgress<DeviceProgress> progress, CancellationToken ct)
        {
            var currentTime = DateTime.Now; // + new TimeSpan(10, 0, 0);
           // var currentTime = new DateTime(2020, 6, 11, 21, 12, 1);
            
            var midnight = DateTime.Today;
             
            var AMOnTimeOffest = TimeSpan.Zero;
            var AMOnDuration = TimeSpan.Zero;

            if (_settings.TimeSettings[0].AMOnTimeOffset != null)
            {
                AMOnTimeOffest = TimeSpan.Parse(_settings.TimeSettings[0].AMOnTimeOffset);
            }

            if(_settings.TimeSettings[0].AMOnDuration != null)
            {
                AMOnDuration = TimeSpan.Parse(_settings.TimeSettings[0].AMOnDuration);
            }

            var OffTime = DateTime.Parse(_settings.TimeSettings[0].OffTime);
            var offTime = DateTime.Today + OffTime.TimeOfDay;

            if (currentTime < PMOnTime)
            {
                if (AMOnTimeOffest > TimeSpan.Zero)
                {
                    // Morning OnTime is specified for this device:
                    if (currentTime >= midnight && currentTime < midnight + AMOnTimeOffest)
                    {
                        await SetDeviceOffAsync(progress);

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
                        await SetDeviceOnAsync(progress);

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
                        await SetDeviceOffAsync(progress);

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
                    await SetDeviceOffAsync(progress);

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

            if (currentTime >= PMOnTime && currentTime < offTime)
            {
                // Turn device On:
                await SetDeviceOnAsync(progress);

                // set up the wait for next event:
                var delaySpan = offTime - currentTime;

                progress?.Report(new DeviceProgress()
                {
                    PType = ProgressType.TRACE,
                    PMessage = $"Wait period started @ {DateTime.Now.ToShortTimeString()}; wait period: {delaySpan} "
                });

                Task.Delay(delaySpan, ct).Wait();

                return;
            }

            if (currentTime >= offTime && currentTime < DateTime.Today.AddDays(1))
            {
                // turn device off:
                await SetDeviceOffAsync(progress);

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


        public async Task<string> GetDeviceStatusAsync()
        {
            var dStatus = "UNKNOWN";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
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

        #region Helper Methods

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
                PMessage = $"Turning {_settings.Id} on at {DateTime.Now}... "
            });

            var response = _settings.Id.EndsWith("00") ? await SimDeviceOnAsync()
                                                       : await DeviceOnAsync();

            if ("Success" != response)
            {
                progress?.Report(new DeviceProgress
                {
                    PType = ProgressType.ALERT,
                    PMessage = $"Failure turning device on. Device: {_settings.Id}. " +
                               $"Device returned following message {Environment.NewLine} {response}"
                });
            }
        }

        /// <summary>
        /// Sets device to Off state and reports the progress
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        private async Task SetDeviceOffAsync(IProgress<DeviceProgress> progress)
        {
            // turn device off:
            progress?.Report(new DeviceProgress()
            {
                PType = ProgressType.INFO,
                PMessage = $"Turning {_settings.Id} off at {DateTime.Now}... "
            });

            var response = _settings.Id.EndsWith("00") ? await SimDeviceOffAsync()
                                                       : await DeviceOffAsync();
            
            if ("Success" != response)
            {
                progress?.Report(new DeviceProgress
                {
                    PType = ProgressType.ALERT,
                    PMessage = $"Failure turning device off. Device: {_settings.Id}. " +
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
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
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
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
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
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
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


        #region Oberon Simulated API Calls!

        private async Task<string> SimPingAsync()
        {
            var pingResponse = "";

            // send a simulated Ping
            await Task.Delay(3000);

            pingResponse = "Success";

            return pingResponse;  
        }

        private async Task<string> SimDeviceOffAsync()
        {
            var offResponse = "Success";

            // send a simulated Device off request
            await Task.Delay(3000);

            return offResponse;
        }

        private async Task<string> SimDeviceOnAsync()
        {
            var onResponse = "Success";

            // send a simulated Device off request
            await Task.Delay(3000);

            return onResponse; 
        }

        #endregion

    }
}
