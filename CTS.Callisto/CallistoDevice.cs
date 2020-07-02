using CTS.Juno.Common;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Callisto
{
    public class CallistoDevice
    {
        public string Id { get; private set; }

        public string Location { get; private set; }

        private string _temperature;
        private string _humidity;
        private string _heatIndex;
        private string _dewpoint;

        private readonly bool _isMetric;

        private readonly CallistoSettings _settings;

        private const int _measurementIntervalInMinutes = 10;

        public CallistoDevice(CallistoSettings deviceSettings, bool isMetric = false)
        {
            _settings = deviceSettings;

            Id = _settings.Id;
            Location = _settings.Location;
            _isMetric = isMetric;
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

                var pingresponse = (!_settings.Id.EndsWith("00")) ? await PingAsync()
                                                        : await SimPingAsync();

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
                                   $"Please check this device and make sure that it is still online"

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
                    var response = !_settings.Id.EndsWith("00") ? await PingAsync()
                                              : await SimPingAsync();

                    if (response == "Success")
                    {
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.INFO,
                            PMessage = $"Ping Acknowledged by Callisto device: {_settings.Id}{Environment.NewLine}"
                        });
                    }
                    else
                    {
                        // Device has failed to respond to the Ping request
                        progress?.Report(new DeviceProgress()
                        {
                            PType = ProgressType.ALERT,
                            PMessage = $"Callisto device with Ip Address {_settings.IpAddress} is not responding to the Pings!" +
                                       $"Please make sure this device is still online"

                        });
                    }
                }
            }
        }

        /// <summary>
        /// Launches a monitor routine for this callisto device that periodically makes the API
        /// calls to the device and retrieves latest device measurements. Measurements are returned
        /// to the calling method via ProcessMesuarements Action. 
        /// </summary>
        /// <param name="ProcessMeasurements"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        public async Task StartMonitorRoutineAsync(Action<CallistoMeasurements> ProcessMeasurements, 
                                                   IProgress<DeviceProgress> progresss, CancellationToken cToken)
        {
            progresss?.Report(new DeviceProgress
            {
                PType = ProgressType.INFO,
                PMessage = $"Starting Monitor routine for device: {Id}"
            });

            while (!cToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                progresss?.Report(new DeviceProgress
                {
                    PType = ProgressType.TRACE,
                    PMessage = $"Getting new measurements for device: {Id}"
                });

                if (Id.EndsWith("00"))
                {
                    // Simulated device
                    if (await SimGetMesurements())
                    {
                        progresss?.Report(new DeviceProgress
                        {
                            PType = ProgressType.INFO,
                            PMessage = $"Measurements (simulated) successfully updated for device: {Id}"
                        });

                        // send the measurements back:
                        ProcessMeasurements?.Invoke(new CallistoMeasurements
                        {
                            ReportingDeviceId = Id,
                            Temperature = _temperature,
                            HeatIndex = _heatIndex,
                            Humidity = _humidity,
                            DewPoint = _dewpoint,
                            MeasurementTime = now
                        });
                    }
                }
                else
                {
                    if (await GetMesurements())
                    {
                        progresss?.Report(new DeviceProgress
                        {
                            PType = ProgressType.INFO,
                            PMessage = $"Measurements successfully updated for device: {Id}"
                        });

                        // send the measurements back:
                        ProcessMeasurements?.Invoke(new CallistoMeasurements
                        {
                            ReportingDeviceId = Id,
                            Temperature = _temperature,
                            HeatIndex = _heatIndex,
                            Humidity = _humidity,
                            DewPoint = _dewpoint,
                            MeasurementTime = now
                        });
                    }
                    else
                    {
                        // one more measurements have failed. Send a progress report:
                        progresss?.Report(new DeviceProgress
                        {
                            PType = ProgressType.ALERT,
                            PMessage = $"Failed to update measurements for device: {Id} at " +
                                           $"{now.ToShortDateString() + now.ToLongTimeString()}"
                        });
                    }
                }

                // Fire off a delay of 10 minutes before getting next data reading:
                await Task.Delay(TimeSpan.FromMinutes(_measurementIntervalInMinutes), cToken);
            }     
        }

        #region Callisto API Helper Methods

        private async Task<bool> GetMesurements()
        {
            if (await GetTemperature() == "Success" &&
                     await GetHumidity() == "Success" &&
                     await GetHeatIndex() == "Success")
            {
                if (double.TryParse(_temperature, out double temp) &&
                    double.TryParse(_humidity, out double humidity))
                {
                    _dewpoint = _isMetric ? ComputeDewPointC(temp, humidity)
                                          : ComputeDewPointF(temp, humidity);
                }

                return true;
            }
            else
                return false;
        }

        private async Task<bool> SimGetMesurements()
        {
            await Task.Delay(TimeSpan.FromSeconds(2)); // simulated measurement delay

            Random r = new Random();

            _temperature = r.Next(-25, 125).ToString();
            _heatIndex = r.Next(-25, 125).ToString();
            _humidity = r.Next(0, 100).ToString();
            
            if (double.TryParse(_temperature, out double temp) &&
                double.TryParse(_humidity, out double humidity))
            {
                _dewpoint = _isMetric ? ComputeDewPointC(temp, humidity)
                                      : ComputeDewPointF(temp, humidity);
            }

            return true;
        }

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

        private static async Task<string> SimPingAsync()
        {
            // send a simulated Ping
            await Task.Delay(3000);

            string pingResponse = "Success";

            return pingResponse;
        }

        private async Task<string> GetTemperature()
        {
            var response = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var res = _isMetric ? await client.GetAsync("/TemperatureC")
                                        : await client.GetAsync("/TemperatureF");

                    if (res.IsSuccessStatusCode)
                    {
                        // read the content from the device:
                        var readTask = res.Content.ReadAsStringAsync();

                        // Open the HTML envelop returned by the device and set
                        // the current temp value:
                        var tempData = OpenHTMLEnvelop(readTask.Result);

                        _temperature = tempData[1];

                        response = "Success";
                    }
                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    response = x.Message;
                }

                return response;
            }
        }

        private async Task<string> GetHumidity()
        {
            var response = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var res = await client.GetAsync("/Humidity");


                    if (res.IsSuccessStatusCode)
                    {
                        // read the content from the device:
                        var readTask = res.Content.ReadAsStringAsync();

                        // Open the HTML envelop returned by the device and set
                        // the current temp value:
                        var HumidityData = OpenHTMLEnvelop(readTask.Result);

                        _humidity = HumidityData[1];

                        response = "Success";
                    }

                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    response = x.Message;
                }

                return response;
            }
        }

        private async Task<string> GetHeatIndex()
        {
            var response = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{_settings.IpAddress}");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.Timeout = TimeSpan.FromMilliseconds(10000);

                try
                {
                    var res = _isMetric ? await client.GetAsync("/HeatIndexC")
                                        : await client.GetAsync("/HeatIndexF");

                    if (res.IsSuccessStatusCode)
                    {
                        // read the content from the device:
                        var readTask = res.Content.ReadAsStringAsync();

                        // Open the HTML envelop returned by the device and set
                        // the current temp value:
                        var HeatXData = OpenHTMLEnvelop(readTask.Result);

                        // TODO: Fix in the arduino code for heatIndex return (label should be "HeatIndex" not "Heat Index"
                        _heatIndex = HeatXData[2];

                        response = "Success";
                    }

                }
                catch (Exception x)
                {
                    // the request takes longer than 10 secs, it is timed out
                    response = x.Message;
                }

                return response;
            }
        }


        #endregion


        #region General Helper Methods


        /// <summary>
        /// returns an approximation of the dew point temperature in Fahrenheit for the given
        /// values of temperature (in Fahrenheit) and humidity. Humidity must be 
        /// above 50% for the equation to work. If humidity is lower than 50% then 
        /// method returns "unavailable"
        /// </summary>
        /// <param name="temperature in degrees F"></param>
        /// <param name="humidity"></param>
        /// <returns></returns>
        private static string ComputeDewPointF(double temperatureF, double humidity)
        {
            if (humidity > 50) // The following approximation is good only when the Humidity > 50%
            {
                var dewpoint = temperatureF - ((100 - humidity) * 9 / 25);

                return dewpoint.ToString("F", CultureInfo.InvariantCulture);
            }
            else
            {
                return "N/A";
            }
        }

        /// <summary>
        /// returns an approximation of the dew point temperature in Celsius for the given
        /// values of temperature (in Celsius) and humidity. Humidity must be 
        /// above 50% for the equation to work. If humidity is lower than 50% then 
        /// method returns "unavailable"
        /// </summary>
        /// <param name="temperature in degrees C"></param>
        /// <param name="humidity"></param>
        /// <returns></returns>
        private static string ComputeDewPointC(double temperatureC, double humidity)
        {
            if (humidity > 50) // The following approximation is good only when the Humidity > 50%
            {
                var dewpoint = temperatureC - ((100 - humidity) / 5);

                return dewpoint.ToString();
            }
            else
            {
                return "N/A";
            }
        }

        /// <summary>
        /// Opens the HTML envelop which contains the actual data returned
        /// by the device and returns an three element array of strings:
        /// element0 : Name of the data
        /// element1 : Value of the data
        /// element2 : units of the data
        /// </summary>
        private static string[] OpenHTMLEnvelop(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex).Trim().Split(' ');
        } 
        #endregion
    }
}
