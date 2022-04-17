using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PInvoke;

namespace sample_shutdown_event_service_fw
{
    internal class NativeMethods
    {


        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_PRESHUTDOWN_INFO
        {
            public uint dwPreshutdownTimeout;
        }


        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool ChangeServiceConfig2(AdvApi32.SafeServiceHandle hService, AdvApi32.ServiceInfoLevel dwInfoLevel, ref SERVICE_PRESHUTDOWN_INFO lpInfo);
    }
}
