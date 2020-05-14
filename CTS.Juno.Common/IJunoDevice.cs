using System;

namespace CTS.Juno.Common
{
    /// <summary>
    /// config interface to read device settings from the appsettings.json file
    /// </summary>
    public interface IJunoDevice
    {
        string Id { get; set; }

        string Name { get; set; }
   
        string SerialNumber { get; set; }

        DateTime? ProvisionDate { get; set; }

        string IpAddress { get; set; }
        
        string Location { get; set; }

    }
}
