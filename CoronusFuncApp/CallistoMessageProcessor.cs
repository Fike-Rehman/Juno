using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;
using TableAttribute = Microsoft.Azure.WebJobs.TableAttribute;

namespace CoronusFuncApp
{
    public class CallistoMessageProcessor
    {
        private static HttpClient client = new HttpClient();

        private readonly TelemetryClient telemetryClient;

        public CallistoMessageProcessor(TelemetryConfiguration configuration)
        {
            telemetryClient = new TelemetryClient(configuration);

            telemetryClient.InstrumentationKey = "40795732-b450-4c69-815c-64d46a94ba45";
        }

        [FunctionName("OnCallistoMessage")]
        [return: Table("Callisto", Connection = "CALLISTO_TABLE_CONNECTIONSTRING")]
        public CallistoMeasurementsEntity Run( [IoTHubTrigger("messages/events", Connection = "ConnectionString")]EventData message, 
                                                               ILogger log)
        {
            // De-serialize incoming payload:
            var callistoData = JsonConvert.DeserializeObject<CallistoData>(Encoding.UTF8.GetString(message.Body.Array));

            // put the data in table storage using the output binding. Generate a row key based on time stamp such that the 
            // latest readings go on the top of the table.
            var callistoEntity = new CallistoMeasurementsEntity
            {
                PartitionKey = "CallistoMeasurment",
                RowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d20"),
                DeviceId = callistoData.deviceId,
                Device_Location = callistoData.callistoDeviceData.Device_Location,
                Device_ReportingTime = callistoData.callistoDeviceData.Device_ReportingTime,
                Meausurement_Temperature = callistoData.callistoDeviceData.Meausurement_Temperature,
                Meausurement_HeatIndex = callistoData.callistoDeviceData.Meausurement_HeatIndex,
                Meausurement_Humidity =callistoData.callistoDeviceData.Meausurement_Humidity,
                Meausurement_DewPoint =callistoData.callistoDeviceData.Meausurement_DewPoint
            };

            var msgString = string.Format("Processed Callisto device message from Zohal IOT Hub. Device Id: {0}", callistoData.deviceId);

            log.LogInformation(msgString);

            telemetryClient.TrackEvent(msgString);

            return callistoEntity;
        }
    }

    

    
}