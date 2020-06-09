using System;

namespace CTS.Juno.Common
{

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

       // public DateTime PReportTime { get; set; }

        public string PMessage { get; set; }
    }

}
