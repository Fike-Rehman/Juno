using System;

namespace CoronusFuncApp
{
    /// <summary>
    /// Entity defined to Utarid Table Storage
    /// </summary>
    public class CallistoMeasurementsEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string DeviceId { get; set; }

        public string Device_Location { get; set; }

        public DateTime Device_ReportingTime { get; set; }

        public string Meausurement_Temperature { get; set; }

        public string Meausurement_HeatIndex { get; set; }

        public string Meausurement_Humidity { get; set; }

        public string Meausurement_DewPoint { get; set; }
    }
}
