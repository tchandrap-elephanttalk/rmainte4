#define RunAsService

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Ipc;
using System.Collections;
using System.Security.Principal;
using System.Security.AccessControl;

using System.Diagnostics; // EventLog
using System.ServiceProcess; // Service


namespace rmainte4
{
    class Program
    {
        static void Main(string[] args)
        {
#if RunAsService
            ServiceBase[] ServicesToRun;

            // �����̃��[�U�[ �T�[�r�X�������v���Z�X�Ŏ��s����Ă���\��������܂��B
            // ���̃v���Z�X�ɂ��� 1 �T�[�r�X��ǉ�����ɂ́A���̍s��ύX���� 2 �Ԗڂ�
            // �T�[�r�X �I�u�W�F�N�g���쐬���Ă��������B���Ƃ��΁A�ȉ��̂Ƃ���ł��B
            //
            //   ServicesToRun = new ServiceBase[] {new Service1(), new MySecondUserService()};
            //
            ServicesToRun = new ServiceBase[] { new RmainteService() };

            ServiceBase.Run(ServicesToRun);
#else
            StartAsConsoleApp();
#endif
        }

        private static void StartAsConsoleApp()
        {
            try
            {
                RemotingServer.Instance.Register(true, true, true);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("�\�P�b�g�G���[���������܂����B���Ƀ|�[�g���g���Ă��Ȃ����m�F���Ă��������B");
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();

            RemotingServer.Instance.Unregister();
        }
    }



    // �V���O���g��
    // ���C���̃T�[�o�N���X
    public sealed class RemotingServer
    {
        private static readonly RemotingServer _instance = new RemotingServer();

        // Singleton
        private RemotingServer()
        {
        }

        public static RemotingServer GetInstance()
        {
            return _instance;
        }

        public static RemotingServer Instance
        {
            get { return _instance; }
        }


        /// <summary>
        /// Register/Unregister����Http�`���l��
        /// </summary>
        private IChannel _httpChannel = null;

        /// <summary>
        /// Register/Unregister����Tcp�`���l��
        /// </summary>
        private IChannel _tcpChannel = null;

        /// <summary>
        /// Register/Unregister����IPC�`���l��
        /// </summary>
        private IChannel _ipcChannel = null;


        public void Register(bool useIPC, bool useTCP, bool useHTTP)
        {
            // �f�[�^�x�[�X�̕���
            try
            {
                Database.Instance.Load();
            }
            catch (Exception)
            {
                // �ŏ��͕K�����s����
            }

            // IPC���g���ƁA�R�l�N�V�������c����ςȂ��ɂȂ����Ƃ��ɁA�s����N����B
            // PC���X�^���o�C������Ƃ܂����̂ŁAIPC�͎g��Ȃ����������B
            if (useIPC)
            {
                // IPC���g���Ƃ��́A�T�[�o����ACL��ݒ肵�Ȃ��ƃ_���݂����B
                // �����̃T���v�����R�s�[���Ďg�p�B
                // http://msdn2.microsoft.com/en-us/library/ms180985(vs.80).aspx

                IDictionary props = new Hashtable();
                props["portName"] = AppSettings.Instance.ObjectUri;

                // This is the wellknown sid for network sid
                string networkSidSddlForm = @"S-1-5-2";

                // Local administrators sid
                SecurityIdentifier localAdminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                // Local Power users sid
                SecurityIdentifier powerUsersSid = new SecurityIdentifier(WellKnownSidType.BuiltinPowerUsersSid, null);

                // Network sid
                SecurityIdentifier networkSid = new SecurityIdentifier(networkSidSddlForm);

                DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);

                // Disallow access from off machine
                dacl.AddAccess(AccessControlType.Deny, networkSid, -1, InheritanceFlags.None, PropagationFlags.None);

                // Allow acces only from local administrators and power users
                dacl.AddAccess(AccessControlType.Allow, localAdminSid, -1, InheritanceFlags.None, PropagationFlags.None);
                dacl.AddAccess(AccessControlType.Allow, powerUsersSid, -1, InheritanceFlags.None, PropagationFlags.None);

                CommonSecurityDescriptor securityDescriptor =
                    new CommonSecurityDescriptor(false, false,
                            ControlFlags.GroupDefaulted |
                            ControlFlags.OwnerDefaulted |
                            ControlFlags.DiscretionaryAclPresent,
                            null, null, null, dacl);

                // IPC Channel���쐬
                _ipcChannel = new IpcServerChannel(props, null, securityDescriptor);
                ChannelServices.RegisterChannel(_ipcChannel, false);
            }

            if (useHTTP)
            {
                // HTTP Channel���쐬
                _httpChannel = new HttpServerChannel(AppSettings.Instance.HttpPort);
                ChannelServices.RegisterChannel(_httpChannel, false);
            }

            if (useTCP)
            {
                // TCP Channel���쐬
                _tcpChannel = new TcpServerChannel(AppSettings.Instance.TcpPort);
                ChannelServices.RegisterChannel(_tcpChannel, true);
            }

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(IRmainteImpl), AppSettings.Instance.ObjectUri, WellKnownObjectMode.Singleton);
        }

        public void Unregister()
        {
            Database.GetInstance().WriteXml();

            if (_ipcChannel != null)
            {
                ChannelServices.UnregisterChannel(_ipcChannel);
            }

            if (_httpChannel != null)
            {
                ChannelServices.UnregisterChannel(_httpChannel);
            }

            if (_tcpChannel != null)
            {
                ChannelServices.UnregisterChannel(_tcpChannel);
            }
        }
    }


    // �T�[�r�X�Ƃ��ċN�����邽�߂̃N���X
    public partial class RmainteService : ServiceBase
    {
        public RmainteService()
        {
            _eventLog = new System.Diagnostics.EventLog();
            _eventLog.Log = "Application";
            _eventLog.Source = AppSettings.Instance.EventSource; // Rmainte4
        }

        private EventLog _eventLog = null;
        private void WriteEventLog(string message)
        {
            _eventLog.WriteEntry(DateTime.Now.ToString() + Environment.NewLine + message);
        }

        protected override void OnStart(string[] args)
        {
            // ���̒��Ŕ���������O�͎����I�ɃC�x���g���O�ɋL�^�����B
            // �Ȃ̂ł킴��catch���Ȃ��B

            // ���s�p�X�̎擾��Windows�T�[�r�X�ł��L���̂悤���B
            // _eventLog.WriteEntry(System.Windows.Forms.Application.ExecutablePath);
            RemotingServer.Instance.Register(true, true, true);
        }

        protected override void OnStop()
        {
            RemotingServer.Instance.Unregister();
        }
    }

}
