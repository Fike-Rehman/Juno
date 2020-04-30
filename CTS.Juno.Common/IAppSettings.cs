using System;
using System.Collections.Generic;
using System.Text;

namespace CTS.Juno.Common
{
    public interface IAppSettings
    {
        string ZohalHubUri { get; }

        Devicelist Devicelist { get; }
    }
}
