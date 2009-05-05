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
    /// WorkItemState�񋓑�
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
            string desc = (isJa) ? "�쐬�ς�" : "Created";
            switch (state)
            {
                case WorkItemState.Created:
                    desc = (isJa) ? "�쐬�ς�" : "Created";
                    break;
                case WorkItemState.Scheduled:
                    desc = (isJa) ? "�X�P�W���[���ς�" : "Scheduled";
                    break;
                case WorkItemState.Queued:
                    desc = (isJa) ? "���[�N�L���[" : "Queued";
                    break;
                case WorkItemState.Running:
                    desc = (isJa) ? "���s��" : "Running";
                    break;
                case WorkItemState.Failed:
                    desc = (isJa) ? "���s" : "Failed";
                    break;
                case WorkItemState.Completed:
                    desc = (isJa) ? "����" : "Completed";
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
