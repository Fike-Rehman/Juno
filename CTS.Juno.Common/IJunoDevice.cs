using System;

namespace CTS.Juno.Common
{
    public interface IJunoDevice
    {
        string Id { get; set; }

        string Name { get; set; }
   
        string SerialNumber { get; set; }

        DateTime? ProvisionDate { get; set; }

        string IpAddress { get; set; }
        
        string Location { get; set; }

     //   string DeviceKey { get; set; }
    }
}
