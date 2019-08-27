using Newtonsoft.Json;
using System;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {
        [JsonProperty(PropertyName = "Id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "IpAddress")]
        public string IpAddress { get; set; }

        [JsonProperty(PropertyName = "Location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "OnTimeOffset")]
        public TimeSpan OnTimeOffset { get; set; }

        [JsonProperty(PropertyName = "OffTime")]
        public TimeSpan OffTime { get; set; }
    }

    public enum PingResult
    {
        OK,
        FAILURE,
        CANCELLED
    }
}
