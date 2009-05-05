// #define TEST_PINGER

using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Threading;
using System.IO;

using rmainte4.TimelineControl;

namespace rmainte4
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ThreadExceptionイベント・ハンドラを登録して、Formの中で取りこぼした例外をキャッチする
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            // UnhandledExceptionイベント・ハンドラを登録して、取りこぼした例外をキャッチする
            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(Application_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#if TEST_PINGER
            Application.Run(new rmainte4.PingMonitor.PingerT());
#else
            Application.Run(MainForm.GetInstance());
#endif
        }

        // 未処理例外をキャッチするイベント・ハンドラ
        // （Windowsアプリケーション用）
        public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowErrorMessage(e.Exception, "Application_ThreadException");
        }

        // 未処理例外をキャッチするイベント・ハンドラ
        // （主にコンソール・アプリケーション用）
        public static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            if (ex != null)
            {
                ShowErrorMessage(ex, "Application_UnhandledException");
            }
        }

        // エラーメッセージの表示
        public static void ShowErrorMessage(Exception ex, string extraMessage)
        {
            String cr = Environment.NewLine;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(extraMessage);
            sb.Append(cr);
            sb.Append(" ――――――――");
            sb.Append(cr + cr);
            sb.Append("エラーが発生しました。開発元にお知らせください");
            sb.Append(cr + cr);
            sb.Append("【エラー内容】" + cr + ex.Message + cr + cr);
            sb.Append("【スタックトレース】" + cr);
            sb.Append(ex.ToString());

            MessageBox.Show(sb.ToString());

            // ErrorForm f = new ErrorForm(sb.ToString());
            // f.ShowDialog();
            // f.Dispose();
        }

    }
}