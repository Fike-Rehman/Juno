using System;

namespace CoronusFuncApp
{
    /// <summary>
    /// provides a de-serializaiton object to process incoming events from
    /// the Zohal IOT Hub
    /// </summary>
    public class CallistoData
    {
        public string deviceId { get; set; }

        public CallistoDevicePayload callistoDeviceData { get; set; }
    }

    public class CallistoDevicePayload
    {
        public string Device_Location { get; set; }

        public DateTime Device_ReportingTime { get; set; }

        public string Meausurement_Temperature { get; set; }

        public string Meausurement_HeatIndex { get; set; }

        public string Meausurement_Humidity { get; set; }

        public string Meausurement_DewPoint { get; set; }
    }
}
