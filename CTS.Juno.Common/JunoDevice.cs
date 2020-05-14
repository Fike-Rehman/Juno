using Newtonsoft.Json;
using System;

namespace CTS.Juno.Common
{
    /// <summary>
    /// config class used to read in the device configurations from the appsetings.json file
    /// </summary>
    public class JunoDevice : IJunoDevice
    {
        [JsonProperty(PropertyName = "Id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Name", Required = Required.AllowNull)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "SerialNumber", Required = Required.Always)]
        public string SerialNumber { get; set; }

        [JsonProperty(PropertyName = "ProvisionDate", Required = Required.Always)]
        public DateTime? ProvisionDate { get; set; }

        [JsonProperty(PropertyName = "IpAddress", Required = Required.Always)]
        public string IpAddress { get; set; }

        [JsonProperty(PropertyName = "Location", Required = Required.Always)]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "Settings", Required = Required.AllowNull)]
        public DeviceSettings[] Settings { get; set; }
    }


    public class DeviceSettings
    {
        public string AMOnTimeOffset { get; set; }

        public string AMOnDuration { get; set; }

        public string OnTimeOffset { get; set; }

        public string OffTime { get; set; }
    }    
}
