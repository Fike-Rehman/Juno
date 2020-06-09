namespace CTS.Juno.Common
{
    public interface IAppSettings
    {
        string ZohalHubUri { get; set; }

        bool IsMetric { get; set; }

        Devicelist Devicelist { get; set; }
    }
}
