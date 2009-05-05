using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration.Install;

namespace rmainte4
{
    [RunInstaller(true)]
    public class RmainteServiceInstaller : Installer
    {
        private ServiceInstaller _installer;
        private ServiceProcessInstaller _processInstaller;

        public RmainteServiceInstaller()
        {
            _installer = new ServiceInstaller();
            _installer.StartType = System.ServiceProcess.ServiceStartMode.Manual;
            _installer.ServiceName = "Rmainte4";
            _installer.DisplayName = "Rmainte4";
            _installer.Description = "Service for Network Devices Remote Maintenance Version 4";
            _installer.StartType = ServiceStartMode.Automatic;
            Installers.Add(_installer);
            _processInstaller = new ServiceProcessInstaller();
            _processInstaller.Account = ServiceAccount.LocalSystem;
            Installers.Add(_processInstaller);
        }
    }
}
