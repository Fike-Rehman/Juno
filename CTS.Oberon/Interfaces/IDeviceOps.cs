using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public interface IDeviceOps
    {
        Task<PingResult> DevicePingAsync(string deviceIp, IProgress<string> progress, CancellationToken ct);

        Task StartPingRoutine(IProgress<string> progress, CancellationToken cToken);
    }
}

