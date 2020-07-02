using CTS.Juno.Common;
using System;

namespace CTS.Oberon
{
    public class OberonSettings
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string SerialNumber { get; set; }

        public DateTime? ProvisionDate { get; set; }

        public string IpAddress { get; set; }

        public string Location { get; set; }

        public DeviceSettings[] TimeSettings { get; set; }
    }
}
