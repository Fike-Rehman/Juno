using System.Threading;

namespace CTS.Juno.Common
{
    public interface IDeviceEngine
    {
        void Run(CancellationToken cToken);
    }
}
