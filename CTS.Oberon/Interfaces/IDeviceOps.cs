using System.Threading;
using System.Threading.Tasks;

namespace CTS.Oberon
{
    public interface IDeviceOps
    {
        Task<PingResult> DevicePingAsync(string deviceIp, CancellationToken ct);

        void StartPingRoutine();
    }
}

