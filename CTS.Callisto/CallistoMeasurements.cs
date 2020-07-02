using System;

namespace CTS.Callisto
{
    public class CallistoMeasurements
    {
        public string ReportingDeviceId { get; set; }

        public string Temperature { get; set; } = "Initial";

        public string Humidity { get; set; } = "Initial";

        public string HeatIndex { get; set; } = "Initial";

        public string DewPoint { get; set; } = "N/A";

        public DateTime MeasurementTime { get; set; } = DateTime.MinValue;
    }
}
