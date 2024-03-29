﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PInvoke;
using System.Runtime.InteropServices;
using System.Threading;

namespace sample_shutdown_event_service_fw
{
    public partial class SampleFWService : ServiceBase
    {
        private static readonly Logger m_Log = LogManager.GetCurrentClassLogger();

        public SampleFWService()
        {
            InitializeComponent();

            try
            {
                {
                    FieldInfo acceptedCommandsFieldInfo =
                        typeof(ServiceBase).GetField("acceptedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (acceptedCommandsFieldInfo == null)
                        throw new Exception("acceptedCommands field not found");

                    int value = (int)acceptedCommandsFieldInfo.GetValue(this);
                    acceptedCommandsFieldInfo.SetValue(this, value | (int)ServiceStatusControls.SERVICE_ACCEPT_PRESHUTDOWN);
                }
            }
            catch (Exception e)
            {
                m_Log.Warn($"SERVICE_ACCEPT_PRESHUTDOWN Set Fail, {e}");
            }
        }


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


        protected override void OnStart(string[] args)
        {

            try
            {
                using (var hSCM = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS))
                using (var hService = AdvApi32.OpenService(hSCM, ServiceName, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS))
                {
                    var info = new NativeMethods.SERVICE_PRESHUTDOWN_INFO
                    {
                        dwPreshutdownTimeout = 1 * 60 * 1000,
                    };
                    if (!NativeMethods.ChangeServiceConfig2(hService, AdvApi32.ServiceInfoLevel.SERVICE_CONFIG_PRESHUTDOWN_INFO, ref info))
                    {
                        var win32Err = Marshal.GetLastWin32Error();
                        throw new Exception($"ChangeServiceConfig2 Fail, Win32Err=0x{win32Err:X}");
                    }

                    m_Log.Info($"ChangeServiceConfig2(SERVICE_CONFIG_PRESHUTDOWN_INFO) Success");
                }
            }
            catch (Exception e)
            {
                m_Log.Warn($"ChangeServiceConfig2 Fail, {e}");
            }

        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            m_Log.Info($"{changeDescription.Reason}");
            base.OnSessionChange(changeDescription);
        }

        protected override void OnCustomCommand(int command)
        {
            m_Log.Info($"{command}");

            switch ((AdvApi32.ServiceControl)command)
            {
                case AdvApi32.ServiceControl.SERVICE_CONTROL_PRESHUTDOWN:

                    try
                    {
                        var status = new AdvApi32.SERVICE_STATUS
                        {
                            dwCurrentState = AdvApi32.ServiceState.SERVICE_STOP_PENDING,
                            dwWaitHint = 1 * 60 * 1000,
                            dwCheckPoint = 0,
                            dwServiceType = AdvApi32.ServiceType.SERVICE_WIN32_OWN_PROCESS,
                        };
                        if (!AdvApi32.SetServiceStatus(ServiceHandle, ref status))
                        {
                            var win32Err = Marshal.GetLastWin32Error();
                            throw new Exception($"SetServiceStatus(SERVICE_STOP_PENDING), Fail, Win32Err=0x{win32Err:X}");
                        }

                        m_Log.Info($"Test Wait Start");
                        for (int i = 0; i < 80; i++)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            m_Log.Info($"Test Wait {i}[s]");
                        }
                        m_Log.Info($"Test Wait End");


                        status.dwCurrentState = AdvApi32.ServiceState.SERVICE_STOPPED;
                        status.dwWaitHint = 0;
                        if (!AdvApi32.SetServiceStatus(ServiceHandle, ref status))
                        {
                            var win32Err = Marshal.GetLastWin32Error();
                            throw new Exception($"SetServiceStatus(SERVICE_STOPPED), Fail, Win32Err=0x{win32Err:X}");
                        }


                    }
                    catch (Exception e)
                    {
                        m_Log.Warn($"SERVICE_CONTROL_PRESHUTDOWN Fail, {e}");
                    }

                    break;
            }

        }

        protected override void OnStop()
        {
        }
    }
}
