using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.IO;
using System.ComponentModel;

namespace Tcp.Chat
{
    public class Client : Tcp.Client
    {
        private BackgroundWorker m_worker;

        public delegate void ConnectionStateHandler(ConnectionStates connectionState);
        public event ConnectionStateHandler ConnectionStateChanged;

        public enum ConnectionStates
        {
            Disconnected,
            Connecting,
            Connected
        }
        private ConnectionStates m_connectionState = ConnectionStates.Disconnected;
        public ConnectionStates ConnectionState
        {
            get { return this.m_connectionState; }
            private set
            {
                this.m_connectionState = value;
                if (ConnectionStateChanged != null) ConnectionStateChanged(value);
            }
        }

        private String m_host;
        private int m_port;

        private bool m_connector;

        private String m_nickname;
        public String Nickname
        {
            set
            {
                this.m_nickname = value;
                SetMyInfo();
            }
            get { return this.m_nickname; }
        }

        private String m_status;
        public String Status
        {
            set
            {
                this.m_status = value;
                SetMyInfo();
            }
            get { return this.m_status; }
        }

        public delegate void MessageReceivedHandler(String message);
        public event MessageReceivedHandler MessageReceived;

        public delegate void ContactNicknameChangedHandler(String name);
        public event ContactNicknameChangedHandler ContactNicknameChanged;

        public delegate void ContactStatusChangedHandler(String status);
        public event ContactStatusChangedHandler ContactStatusChanged;

        public Client(String host, int port)
        {
            this.m_host = host;
            this.m_port = port;

            this.m_connector = true;
        }

        public Client(TcpClient client)
        {
            this.m_client = client;

            this.m_connector = false;
        }

        public void AttachClient(TcpClient client, bool connect)
        {
            Disconnect();
            this.m_client = client;
            Connect(connect);
        }

        protected override void Send(String message)
        {
            message += ChatClient.App.Delimiter;
            base.Send(message);
        }

        private String Read()
        {
            String buffer;
            int tmp;

            buffer = "";
            tmp = this.m_stream.ReadByte();
            while ((char)tmp != ChatClient.App.Delimiter)
            {
                buffer += (char)tmp;
                tmp = this.m_stream.ReadByte();
            }
            System.Diagnostics.Debug.WriteLine(buffer);
            return buffer;
        }

        public void Connect()
        {
            this.m_worker = new BackgroundWorker();
            this.m_worker.WorkerReportsProgress = true;
            this.m_worker.WorkerSupportsCancellation = true;
            this.m_worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.m_worker.RunWorkerAsync();
        }

        public void Connect(bool connector)
        {
            this.m_connector = connector;
            
            this.m_worker = new BackgroundWorker();
            this.m_worker.WorkerReportsProgress = true;
            this.m_worker.WorkerSupportsCancellation = true;
            this.m_worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.m_worker.RunWorkerAsync();
        }

        public void Disconnect()
        {
            if(this.m_client != null)
                this.m_client.Client.Close();
        }

        private void SetMyInfo()
        {
            if (this.m_connectionState != ConnectionStates.Connected)
                return;

            Send(Command.Create(Command.Set, Command.Status, this.m_status));
            Send(Command.Create(Command.Set, Command.Nickname, this.m_nickname));
        }

        private bool DoConnect()
        {
            try
            {
                this.m_worker.ReportProgress(1, ConnectionStates.Connecting);
                this.m_client.Connect(this.m_host, this.m_port);
                return true;
            }
            catch (SocketException ex)
            {
                this.m_worker.ReportProgress(1, ConnectionStates.Disconnected);
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (this.m_connector)
            {
                this.m_client = new TcpClient();

                if (!DoConnect())
                    return;
            }

            this.m_client.NoDelay = true;

            BackgroundWorker worker = (BackgroundWorker)sender;

            worker.ReportProgress(1, ConnectionStates.Connected);

            this.m_stream = this.m_client.GetStream();

            if (this.m_connector)
                Send(Command.Create(Command.Get, Command.Version));

            while (this.m_stream.CanRead)
            {
                String read;
                try
                {
                    read = Read();
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    break;
                }

                if (worker.CancellationPending)
                    break;
                else
                    worker.ReportProgress(1, read);
            }

            worker.ReportProgress(1, ConnectionStates.Disconnected);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            object userState = e.UserState;
            if (userState is ConnectionStates)
            {
                this.ConnectionState = (ConnectionStates)userState;
                return;
            }

            String message = userState.ToString();

            Command cmd = new Command(message);
            switch (cmd.Type)
            {
                case Command.Get:
                    switch (cmd.Cmd)
                    {
                        case Command.Version:
                            Send(Command.Create(Command.Set, Command.Version, "2"));
                            break;
                    }
                    break;
                case Command.Set:
                    switch (cmd.Cmd)
                    {
                        case Command.Version:
                            if (this.m_connector)
                            {
                                SetMyInfo();
                            }
                            break;
                        case Command.Nickname:
                            if (ContactNicknameChanged != null) ContactNicknameChanged(cmd.Value);
                            break;
                        case Command.Status:
                            if (ContactStatusChanged != null) ContactStatusChanged(cmd.Value);
                            break;
                    }
                    break;
                case Command.Send:
                    switch (cmd.Cmd)
                    {
                        case Command.Message:
                            if (MessageReceived != null) MessageReceived(cmd.Value);
                            break;
                    }
                    break;
            }
        }

        public bool SendMessage(String message)
        {
            int count = 0;
            while (this.m_connectionState != ConnectionStates.Connected)
            {
                if (count == 4)
                    break;

                System.Threading.Thread.Sleep(250);
                ++count;
            }

            if (this.m_connectionState == ConnectionStates.Connected)
            {
                Send(Command.Create(Command.Send, Command.Message, message));
                return true;
            }
            return false;
        }
    }
}
