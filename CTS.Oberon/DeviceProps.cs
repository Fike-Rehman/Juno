using Newtonsoft.Json;
using System;

namespace CTS.Oberon
{
    public partial class OberonDevice : IDeviceOps
    {
        [JsonProperty(PropertyName = "Id", Required = Required.Always)]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "Name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "IpAddress", Required = Required.Always)]
        public string IpAddress { get; set; }

        [JsonProperty(PropertyName = "Location", Required = Required.Always)]
        public string Location { get; set; }

        /// <summary>
        /// Timespan from midnight to SONOFF AM on time
        /// Set this to negative if no Morning OnTimne is needed
        /// </summary>
        [JsonProperty(PropertyName = "AMOnTimeOffset")]
        public TimeSpan AMOnTimeOffest { get; set; }

        /// <summary>
        /// Duration to keep the SONOFF On starting from AMOnTimeOffset
        /// This setting is disregarded if AMOnTimeOffset is negative 
        /// </summary>
        [JsonProperty(PropertyName = "AMOnDuration")]
        public TimeSpan AMOnDuration { get; set; }

        /// <summary>
        /// Timespan from today's Sunset Time to turn the SONOFF On
        /// Can be positive (after sunset) or Negative (before sunset)
        /// </summary>
        [JsonProperty(PropertyName = "OnTimeOffset", Required = Required.Always)]
        public TimeSpan OnTimeOffset { get; set; }

        /// <summary>
        /// Exact time when to turn the Sonoff Off each night (must be before midnight)
        /// </summary>
        [JsonProperty(PropertyName = "OffTime", Required = Required.Always)]
        public DateTime OffTime { get; set; }
    }

    public enum PingResult
    {
        OK,
        FAILURE,
        CANCELLED
    }

    /// <summary>
    /// Tells the caller how it should deal with the provided progress
    /// messsage, 
    /// </summary>
    public enum ProgressType : short
    {
        TRACE,
        INFO,
        ALERT
    };

    /// <summary>
    /// object that holds progress information
    /// </summary>
    public struct DeviceProgress
    {
        public ProgressType PType { get; set; }

        public string PMessage { get; set; }
    }
}
