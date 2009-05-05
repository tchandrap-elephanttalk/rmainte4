/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2004.12.27
// Last modified at 2006.07.07
// Last modified at 2009.01.07 for rmainte4

using System;

namespace rmainte4.Threading
{

    /// <summary>
    /// WorkItemState列挙体
    /// </summary>
    public enum WorkItemState
    {
        Created = 0,
        Scheduled,
        Queued,
        Running,
        Failed,
        Completed
    }

    public static class WorkItemStateDescription
    {
        public static string GetDescription(WorkItemState state)
        {
            string desc = (isJa) ? "作成済み" : "Created";
            switch (state)
            {
                case WorkItemState.Created:
                    desc = (isJa) ? "作成済み" : "Created";
                    break;
                case WorkItemState.Scheduled:
                    desc = (isJa) ? "スケジュール済み" : "Scheduled";
                    break;
                case WorkItemState.Queued:
                    desc = (isJa) ? "ワークキュー" : "Queued";
                    break;
                case WorkItemState.Running:
                    desc = (isJa) ? "実行中" : "Running";
                    break;
                case WorkItemState.Failed:
                    desc = (isJa) ? "失敗" : "Failed";
                    break;
                case WorkItemState.Completed:
                    desc = (isJa) ? "完了" : "Completed";
                    break;
                default:
                    break;
            }
            return desc;
        }

        private static bool isJa
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja");
            }
        }

    }
}
