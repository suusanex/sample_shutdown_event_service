using Microsoft.Extensions.Hosting.WindowsServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.ServiceProcess;
using NLog.Fluent;
using System.Diagnostics;
using PInvoke;
using System.Runtime.InteropServices;

namespace sample_shutdown_event_service_worker
{
    internal class SampleServiceLifetime : WindowsServiceLifetime
    {
        private readonly ILogger m_Logger;

        public SampleServiceLifetime(ILogger<SampleServiceLifetime> logger, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor) : base(environment, applicationLifetime, loggerFactory, optionsAccessor)
        {
            m_Logger = logger;
            m_Logger.LogInformation($"Start 1, Timeout={optionsAccessor.Value.ShutdownTimeout}");
        }

        public SampleServiceLifetime(ILogger<SampleServiceLifetime> logger, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor, IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor) : base(environment, applicationLifetime, loggerFactory, optionsAccessor, windowsServiceOptionsAccessor)
        {
            m_Logger = logger;
            m_Logger.LogInformation($"Start 2, Timeout={optionsAccessor.Value.ShutdownTimeout}");


            try
            {
                {
                    var acceptedCommandsFieldInfo =
                        typeof(ServiceBase).GetField("_acceptedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (acceptedCommandsFieldInfo == null)
                        throw new Exception("acceptedCommands field not found");

                    int value = (int)acceptedCommandsFieldInfo.GetValue(this);


                    var changedValue = value | (int)(
                        NativeMethods.ServiceStatusControls.SERVICE_ACCEPT_PRESHUTDOWN);
                    changedValue &= ~(int)NativeMethods.ServiceStatusControls.SERVICE_ACCEPT_SHUTDOWN;

                    acceptedCommandsFieldInfo.SetValue(this, changedValue);

                    m_Logger.LogInformation($"SERVICE_ACCEPT_PRESHUTDOWN Set Success, Get=0x{value:X}, Set=0x{changedValue:X}");

                }
            }
            catch (Exception e)
            {
                m_Logger.LogWarning($"SERVICE_ACCEPT_PRESHUTDOWN Set Fail, {e}");
            }

        }

        protected override void OnStart(string[] args)
        {
            m_Logger.LogInformation($"Start, ServiceName={ServiceName}");
            try
            {
                using var hSCM = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
                using var hService = AdvApi32.OpenService(hSCM, ServiceName, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
                var info = new NativeMethods.SERVICE_PRESHUTDOWN_INFO
                {
                    dwPreshutdownTimeout = 1 * 60 * 1000,
                };
                if (!NativeMethods.ChangeServiceConfig2(hService, AdvApi32.ServiceInfoLevel.SERVICE_CONFIG_PRESHUTDOWN_INFO, ref info))
                {
                    var win32Err = Marshal.GetLastWin32Error();
                    throw new Exception($"ChangeServiceConfig2 Fail, Win32Err=0x{win32Err:X}");
                }

                m_Logger.LogInformation($"ChangeServiceConfig2(SERVICE_CONFIG_PRESHUTDOWN_INFO) Success");
            }
            catch (Exception e)
            {
                m_Logger.LogWarning($"ChangeServiceConfig2 Fail, {e}");
            }
            base.OnStart(args);
        }

        protected override void OnShutdown()
        {
            m_Logger.LogInformation("Start");
            base.OnShutdown();
            m_Logger.LogInformation("Base End");
        }
        

        protected override void OnCustomCommand(int command)
        {
            m_Logger.LogInformation($"{(NativeMethods.ServiceCtrlCode)command}");

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

                        m_Logger.LogInformation($"Test Wait Start");
                        for (int i = 0; i < 80; i++)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            m_Logger.LogInformation($"Test Wait {i}[s]");
                        }
                        m_Logger.LogInformation($"Test Wait End");


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
                        m_Logger.LogWarning($"SERVICE_CONTROL_PRESHUTDOWN Fail, {e}");
                    }

                    break;
            }
            base.OnCustomCommand(command);
        }
    }
}
