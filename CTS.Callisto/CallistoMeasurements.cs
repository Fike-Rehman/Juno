using CTS.Juno.Common;
using System;
using System.Collections.Generic;
using System.Text;

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

    //public class CallistoProgress
    //{
    //    public string ReportingDeviceId { get; set; }

    //    public DateTime ReportTime { get; set; }

    //    public string ReportMessage { get; set; }

    //    public CallistoMeasurements ReportedMesuarements { get; set; }
    //}
}
