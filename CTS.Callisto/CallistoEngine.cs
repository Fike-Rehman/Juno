using CTS.Juno.Common;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace CTS.Callisto
{
    public class CallistoEngine : IDeviceEngine
    {
        private readonly ILogger<CallistoEngine> _logger;

        public CallistoEngine(ILogger<CallistoEngine> logger)
        {
            _logger = logger;
        }

        public void Run(CancellationToken token)
        {
            _logger.LogInformation("Begining Callisto Activities...");
        }
    }
}
