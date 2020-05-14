using System;
using System.Threading;
using System.Threading.Tasks;
using CTS.Juno.Common;

namespace CTS.Oberon
{
    public interface IDeviceOps
    {
        /// <summary>
        /// Initializes this device asynchronously by sending a ping messages. If three conecutive pings fail
        /// PingResult = FAILURE is returned
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task<PingResult> DeviceInitializeAsync(IProgress<DeviceProgress> progress, CancellationToken cToken);

        /// <summary>
        /// Starts the ping routine for this deivce. A ping message is sent every minute. The routine continues
        /// indefinitely until the host serivce is stopped
        /// /// </summary>
        /// <param name="progress"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task StartPingRoutineAsync(IProgress<DeviceProgress> progress, CancellationToken cToken);

        /// <summary>
        /// Starts the Monitor routine that turns device On or Off based on the provided schedule. The routine continues
        /// indefinitely until the host service is stopped.
        /// </summary>
        /// <param name="SunsetToday"></param>
        /// <param name="progress"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        Task StartMonitorRoutineAsync(Func<DateTime> SunsetToday, IProgress<DeviceProgress> progress, CancellationToken cToken);


        /// <summary>
        /// Returns current device status (ON/OFF/UNKNOWN) without touching the SONOFF relays
        /// </summary>
        /// <returns></returns>
        Task<string> GetDeviceStatusAsync();
    }
}

