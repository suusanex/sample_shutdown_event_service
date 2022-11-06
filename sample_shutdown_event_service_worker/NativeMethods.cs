using PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace sample_shutdown_event_service_worker
{
    internal static class NativeMethods
    {
        [Flags]
        internal enum ServiceStatusControls : uint
        {
            None = 0,
            SERVICE_ACCEPT_STOP = 0x00000001,
            SERVICE_ACCEPT_PAUSE_CONTINUE = 0x00000002,
            SERVICE_ACCEPT_SHUTDOWN = 0x00000004,
            SERVICE_ACCEPT_PARAMCHANGE = 0x00000008,
            SERVICE_ACCEPT_NETBINDCHANGE = 0x00000010,
            SERVICE_ACCEPT_HARDWAREPROFILECHANGE = 0x00000020,
            SERVICE_ACCEPT_POWEREVENT = 0x00000040,
            SERVICE_ACCEPT_SESSIONCHANGE = 0x00000080,
            SERVICE_ACCEPT_PRESHUTDOWN = 0x00000100,
            SERVICE_ACCEPT_TIMECHANGE = 0x00000200,
            SERVICE_ACCEPT_TRIGGEREVENT = 0x00000400,
            SERVICE_ACCEPT_USER_LOGOFF = 0x00000800,
        }

        internal enum ServiceCtrlCode
        {
            SERVICE_CONTROL_INTERROGATE = 0x00000004,
            SERVICE_CONTROL_SHUTDOWN = 0x00000005,
            SERVICE_CONTROL_SESSIONCHANGE = 0x0000000E,
            SERVICE_CONTROL_PRESHUTDOWN = 0x0000000F,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_PRESHUTDOWN_INFO
        {
            public uint dwPreshutdownTimeout;
        }


        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool ChangeServiceConfig2(AdvApi32.SafeServiceHandle hService, AdvApi32.ServiceInfoLevel dwInfoLevel, ref SERVICE_PRESHUTDOWN_INFO lpInfo);

    }
}
