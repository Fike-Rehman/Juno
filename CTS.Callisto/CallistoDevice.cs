using System;

namespace CTS.Juno.Common
{
    public class CallistoDevice 
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string SerialNumber { get; set; }

        public DateTime? ProvisionDate { get; set; }

        public string IpAddress { get; set; }

        public string Location { get; set;}

        public string DeviceKey { get; set; }
    }
}
