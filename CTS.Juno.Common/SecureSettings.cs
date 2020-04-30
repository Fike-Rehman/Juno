using Microsoft.Extensions.Configuration;
using System;

namespace CTS.Juno.Common
{
    public class SecureSettings: ISecureSettings
    { 
        private readonly IConfiguration _config;

        public string GetDeviceKey(string deviceId)
        {
            return _config[deviceId] ?? $"Device Key not found for Device Id: {deviceId}";
        }

        public SecureSettings(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
    }
}
