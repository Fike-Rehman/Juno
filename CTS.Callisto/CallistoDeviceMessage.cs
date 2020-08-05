﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CTS.Callisto
{
    public class CallistoDeviceMessage
    {
        public string Device_Location { get; set; }

        public DateTime Device_ReportingTime { get; set; }

        public string Meausurement_Temperature { get; set; }

        public string Meausurement_HeatIndex { get; set; }

        public string Meausurement_Humidity { get; set; }

        public string Meausurement_DewPoint { get; set; }
    }
}
