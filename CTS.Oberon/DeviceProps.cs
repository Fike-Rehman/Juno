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

        /// <summary>
        /// Timespan from midnight to turn the SONOFF on
        /// Set this to negative if no Morning OnTimne is needed
        /// </summary>
        [JsonProperty(PropertyName = "AMOnTimeOffset")]
        public TimeSpan AMOnTimeOffest { get; set; }

        /// <summary>
        /// Duration from AMOnTimeOffeset to keep the SONOFF On
        /// This setting is disregarded if AMOnTimeOffset is negative 
        /// </summary>
        [JsonProperty(PropertyName = "AMOnDuration")]
        public TimeSpan AMOnDuration { get; set; }

        /// <summary>
        /// Timespan from today's Sunset Time to turn the SONOFF On
        /// Can be positive (before sunset) or Negative (After sunset)
        /// </summary>
        [JsonProperty(PropertyName = "OnTimeOffset")]
        public TimeSpan OnTimeOffset { get; set; }

        /// <summary>
        /// Exact time when to turn the Sonoff Off each night (must be before midnight)
        /// </summary>
        [JsonProperty(PropertyName = "OffTime")]
        public DateTime OffTime { get; set; }
    }

    public enum PingResult
    {
        OK,
        FAILURE,
        CANCELLED
    }
}
