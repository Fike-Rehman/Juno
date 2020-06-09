namespace CTS.Juno.Common
{
    public class AppSettings : IAppSettings
    { 
        public string ZohalHubUri { get; set; }

        public bool IsMetric { get; set; }

        public Devicelist Devicelist { get; set; }
    }

    public class Devicelist
    {
        public JunoDevice[] JunoDevices { get; set; }
    }
}
