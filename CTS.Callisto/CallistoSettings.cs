using System;

namespace CTS.Callisto
{
    /// <summary>
    /// CallistoSettings class that holds the config settings
    /// from the app.Settings file for a callisto device
    /// </summary>
    public class CallistoSettings 
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string SerialNumber { get; set; }

        public DateTime? ProvisionDate { get; set; }

        public string IpAddress { get; set; }

        public string Location { get; set; }

        public string DeviceKey { get; set; }
    }
}
