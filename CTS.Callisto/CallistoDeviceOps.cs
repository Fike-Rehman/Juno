using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CTS.Juno.Common;

namespace CTS.Callisto
{
    public partial class CallistoDevice
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

                var pingresponse = (!Id.EndsWith("00")) ? await PingAsync()
                                                        : await SimPingAsync();

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
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(new TimeSpan(0, 0, 1, 0), ct);

                if (!ct.IsCancellationRequested)
                {
                    // if this is a simulated device, send a simulated Ping, otherwise send a real ping
                    var response = Id != "00" ? await PingAsync()
                                              : await SimPingAsync();

                    if (response == "Success")
                    {
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.INFO,
                            PMessage = $"Ping Acknowleged by Callisto device: {Id}, Location {Location}"
                        });
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.ALERT,
                            PMessage = $"Callisto device with Ip Address {IpAddress} is not responding to the Pings!" +
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
        public async Task StartMonitorRoutineAsync(IProgress<DeviceProgress> progress, CancellationToken ct)
        {
            progress.Report(new DeviceProgress()
            {
                PType = ProgressType.INFO,
                PMessage = $"Starting Monitor routine for device: {Id}..."

            });

            while (!ct.IsCancellationRequested)
            {
                await Monitor(progress, ct);
                
            }
        }

        private async Task Monitor(IProgress<DeviceProgress> progress, CancellationToken ct)
        {
            // send requests to get the temp/humidity data from the device
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

        private async Task<string> SimPingAsync()
        {
            var pingResponse = "";

            // send a simulated Ping
            await Task.Delay(3000);

            pingResponse = "Success";

            return pingResponse;
        }
    }
}
