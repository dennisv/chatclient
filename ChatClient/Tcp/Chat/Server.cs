using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.ComponentModel;
using System.Net;

namespace Tcp.Chat
{
    public class Server
    {
        private TcpListener m_server;
        private BackgroundWorker m_worker;

        public delegate void ConnectionAcceptedHandler(TcpClient socket);
        public event ConnectionAcceptedHandler ConnectionAccepted;

        private int m_port;

        private bool m_restart;
        public bool Restart
        {
            set { this.m_restart = value; }
            get { return this.m_restart; }
        }

        public Server(int port)
        {
            this.m_port = port;
        }

        public void Stop()
        {
            if (this.m_worker != null && this.m_worker.IsBusy)
                this.m_worker.CancelAsync();
        }

        public void Listen()
        {
            this.m_worker = new BackgroundWorker();
            this.m_worker.WorkerReportsProgress = true;
            this.m_worker.WorkerSupportsCancellation = true;
            this.m_worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_Completed);
            this.m_worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.m_server = new TcpListener(IPAddress.Any, this.m_port);
                this.m_server.Start();
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Threading.Thread.Sleep(1000 * 60);
                return;
            }

            while (true)
            {
                TcpClient client = this.m_server.AcceptTcpClient();

                BackgroundWorker worker = (BackgroundWorker)sender;
                if (worker.CancellationPending)
                    break;
                else
                    worker.ReportProgress(1, client);
            }

            System.Threading.Thread.Sleep(1000 * 30);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TcpClient client = (TcpClient)e.UserState;

            if (ConnectionAccepted != null) ConnectionAccepted(client);
        }

        private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.m_restart)
                this.m_worker.RunWorkerAsync();
        }
    }
}
