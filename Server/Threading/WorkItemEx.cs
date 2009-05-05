/* *-*-mode:java; -*- */

// Copyright Takamitsu IIDA <gsi@nifty.com>
// CCIE4288
// 2004.12.27
// Last modified at 2006.07.07
// Last modified at 2009.01.07 for rmainte4

using System;
using System.Threading;

namespace rmainte4.Threading
{
    /// <summary>
    /// IWorkItem �̊T�v�̐����ł��B
    /// </summary>
    public interface IWorkItem
    {
        /// <summary>
        /// ���s���郁�\�b�h
        /// </summary>
        void Work();

        /// <summary>
        /// �X���b�h��������~�����Ƃ��̏���
        /// </summary>
        void Terminate();

        /// <summary>
        /// ���[�N�A�C�e�����Ǘ�����X���b�h�v�[��
        /// </summary>
        WorkThreadPool WTPool { get; set; }

        /// <summary>
        /// ���[�N�A�C�e���̏��
        /// </summary>
        WorkItemState State { get; set; }

        /// <summary>
        /// ��O
        /// </summary>
        Exception FailedException { get; set; }

        /// <summary>
        /// �X���b�h�v���C�I���e�B
        /// </summary>
        ThreadPriority Priority { get; set; }

        /// <summary>
        /// �J�n����
        /// </summary>
        DateTime StartedTime { get; set; }

        /// <summary>
        /// �I������
        /// </summary>
        DateTime CompletedTime { get; set; }

        /// <summary>
        /// ���[�NID
        /// </summary>
        int WorkId { get; set; }
    }

    /// <summary>
    /// WorkItem�̃T���v�������B
    /// IWorkItemImpl���p������̂������ʂł́A���̒��g���R�s�[����IWorkItem, ICompararable����������΂悢
    /// </summary>
    public abstract class WorkItemImpl : IWorkItem, IComparable
    {
        // ���ۃN���X�Ȃ̂ŁA�p�����Ďg���K�v������B
        protected WorkItemImpl()
        {
        }

        // ���[�N�A�C�e�������s���钊�ۃ��\�b�h
        // ������I�[�o�[���C�h���Ďg�p
        public abstract void Work();

        // �X���b�h�������I�������Ƃ��Ɏ��s���郁�\�b�h
        public abstract void Terminate();

        // ���̃��[�N�A�C�e�����i�[����Ă���L���[
        // WorkPool��Add()���ꂽ�Ƃ��ɃZ�b�g�����
        public WorkThreadPool WTPool
        {
            get { return (_workPool); }
            set { _workPool = value; }
        }
        private WorkThreadPool _workPool;

        // ���̃��[�N�A�C�e���̏��
        public WorkItemState State
        {
            get { return (_state); }
            set
            {
                WorkItemState prev = _state;
                _state = value;

                switch (value)
                {
                    case WorkItemState.Running:
                        StartedTime = DateTime.Now;
                        break;

                    case WorkItemState.Completed:
                        CompletedTime = DateTime.Now;
                        break;
                }

                // ������Ǘ����Ă���L���[�ɏ�Ԃ̕ω���ʒm
                if (WTPool != null)
                {
                    WTPool.WorkItemStateChanged(this, prev);
                }
            }
        }
        private WorkItemState _state = WorkItemState.Created;

        // ���s���ɔ���������O��ۑ�
        public Exception FailedException
        {
            get { return (_failedException); }
            set { _failedException = value; }
        }
        private Exception _failedException = null;

        // �v���C�I���e�B�B
        // ���̒l�Ń\�[�g����B
        public ThreadPriority Priority
        {
            get { return (_priority); }
            set { _priority = value; }
        }
        private ThreadPriority _priority = ThreadPriority.Normal;

        // �J�n����
        public DateTime StartedTime
        {
            get { return (_startedTime); }
            set { _startedTime = value; }
        }
        private DateTime _startedTime = DateTime.MaxValue;

        // ���s��������
        public DateTime CompletedTime
        {
            get { return (_completedTime); }
            set { _completedTime = value; }
        }
        private DateTime _completedTime = DateTime.MaxValue;

        // �쐬����
        public DateTime CreatedTime
        {
            get { return (_createdTime); }
            set { _createdTime = value; }
        }
        private DateTime _createdTime = DateTime.Now;

        public int WorkId
        {
            get { return (_workId); }
            set { _workId = value; }
        }
        private int _workId = 0;

        // ---------------

        // IComparable�̎���
        public int CompareTo(object obj)
        {
            IWorkItem wi = (IWorkItem)obj;
            if (wi == null)
            {
                throw new ArgumentException("WorkItem�I�u�W�F�N�g�ł͂���܂���");
            }

            return (Priority - wi.Priority);
        }

    }
}
