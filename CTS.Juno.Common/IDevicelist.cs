using System;
using System.Collections.Generic;
using System.Text;

namespace CTS.Juno.Common
{
    public interface IDevicelist
    {
        JunoDevice[] JunoDevices { get; set; }
    }
}
