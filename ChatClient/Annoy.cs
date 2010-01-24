using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ChatClient
{
    public class Annoy
    {
        private BackgroundWorker m_worker;

        private int m_pos;
        private int m_length;

        private String m_name;

        public delegate void AnnoyHandler(String name);
        public event AnnoyHandler Changed;

        public Annoy(String name)
        {
            this.m_name = name;
            this.m_length = name.Length;
            this.m_pos = 0;
        }

        public void Start()
        {
            this.m_worker = new BackgroundWorker();
            this.m_worker.WorkerReportsProgress = true;
            this.m_worker.WorkerSupportsCancellation = true;
            this.m_worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.m_worker.RunWorkerAsync();
        }

        public void Stop()
        {
            this.m_worker.CancelAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;

            while (true)
            {
                /*char[] tmp = this.m_name.ToLower().ToCharArray();
                tmp[this.m_pos] = char.ToUpper(tmp[this.m_pos]);

                if (this.m_pos == this.m_length - 1)
                    this.m_pos = 0;
                else
                    this.m_pos = ++this.m_pos;*/

                this.m_name = this.m_name.Substring(1) + this.m_name.Substring(0, 1);

                if (worker.CancellationPending)
                    break;
                else
                    worker.ReportProgress(1, this.m_name);

                System.Threading.Thread.Sleep(250);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (Changed != null) Changed(e.UserState.ToString());
        }
    }
}
