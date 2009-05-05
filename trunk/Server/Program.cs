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

            // 複数のユーザー サービスが同じプロセスで実行されている可能性があります。
            // このプロセスにもう 1 つサービスを追加するには、次の行を変更して 2 番目の
            // サービス オブジェクトを作成してください。たとえば、以下のとおりです。
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
                Console.WriteLine("ソケットエラーが発生しました。既にポートが使われていないか確認してください。");
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



    // シングルトン
    // メインのサーバクラス
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
        /// Register/UnregisterするHttpチャネル
        /// </summary>
        private IChannel _httpChannel = null;

        /// <summary>
        /// Register/UnregisterするTcpチャネル
        /// </summary>
        private IChannel _tcpChannel = null;

        /// <summary>
        /// Register/UnregisterするIPCチャネル
        /// </summary>
        private IChannel _ipcChannel = null;


        public void Register(bool useIPC, bool useTCP, bool useHTTP)
        {
            // データベースの復元
            try
            {
                Database.Instance.Load();
            }
            catch (Exception)
            {
                // 最初は必ず失敗する
            }

            // IPCを使うと、コネクションが残りっぱなしになったときに、不具合が起こる。
            // PCをスタンバイさせるとまずいので、IPCは使わない方がいい。
            if (useIPC)
            {
                // IPCを使うときは、サーバ側でACLを設定しないとダメみたい。
                // ここのサンプルをコピーして使用。
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

                // IPC Channelを作成
                _ipcChannel = new IpcServerChannel(props, null, securityDescriptor);
                ChannelServices.RegisterChannel(_ipcChannel, false);
            }

            if (useHTTP)
            {
                // HTTP Channelを作成
                _httpChannel = new HttpServerChannel(AppSettings.Instance.HttpPort);
                ChannelServices.RegisterChannel(_httpChannel, false);
            }

            if (useTCP)
            {
                // TCP Channelを作成
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


    // サービスとして起動するためのクラス
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
            // この中で発生した例外は自動的にイベントログに記録される。
            // なのでわざとcatchしない。

            // 実行パスの取得はWindowsサービスでも有効のようだ。
            // _eventLog.WriteEntry(System.Windows.Forms.Application.ExecutablePath);
            RemotingServer.Instance.Register(true, true, true);
        }

        protected override void OnStop()
        {
            RemotingServer.Instance.Unregister();
        }
    }

}
