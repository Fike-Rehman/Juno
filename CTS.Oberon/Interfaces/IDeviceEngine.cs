using System.Threading;

namespace CTS.Oberon
{
    public interface IDeviceEngine
    {
        void Run(CancellationToken cToken);
    }
}
