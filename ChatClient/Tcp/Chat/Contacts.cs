using System;
using System.Collections;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.IO;
using System.ComponentModel;
using System.Xml.Linq;

using Tcp.Chat;

namespace Tcp.Chat
{
    public class Contacts : Tcp.Client
    {
        private BackgroundWorker m_worker;

        public delegate void ConnectionStateHandler(ConnectionStates connectionState);
        public event ConnectionStateHandler ConnectionStateChanged;

        public enum ConnectionStates
        {
            Disconnected,
            Connecting,
            Connected,
            Joined
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
        public String Host
        {
            set { this.m_host = value; }
            get { return this.m_host; }
        }

        private int m_port;
        public int Port
        {
            set { this.m_port = value; }
            get { return this.m_port; }
        }

        private bool m_reconnect;
        public bool Reconnect
        {
            set { this.m_reconnect = value; }
            get { return this.m_reconnect; }
        }

        private int m_reconnectTimeout;
        public int ReconnectTimeout
        {
            set { this.m_reconnectTimeout = value; }
            get { return this.m_reconnectTimeout; }
        }

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

        public delegate void ContactsHandler(ArrayList contacts);
        public event ContactsHandler ContactsList;
        public event ContactsHandler ContactsUpdate;
        public event ContactsHandler ContactsAdd;
        public event ContactsHandler ContactsDelete;

        public Contacts(String host, int port)
        {
            this.Nickname = "NoName";
            this.Status = "online";
            this.Reconnect = true;

            this.m_host = host;
            this.m_port = port;
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
            this.m_worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_Completed);
            this.m_worker.RunWorkerAsync();
        }

        public void Disconnect()
        {
            if (this.m_client != null)
            {
                Send(Command.Create(Command.Send, Command.Leave));
                this.m_client.Client.Close();
            }
        }

        private void SetMyInfo()
        {
            if (this.m_connectionState != ConnectionStates.Joined)
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
            this.m_client = new TcpClient();
            this.m_client.NoDelay = true;

            if (!DoConnect())
            {
                System.Threading.Thread.Sleep(1000 * this.m_reconnectTimeout);
                return;
            }

            BackgroundWorker worker = (BackgroundWorker)sender;

            worker.ReportProgress(1, ConnectionStates.Connected);

            this.m_stream = this.m_client.GetStream();

            Send(Command.Create(Command.Request, Command.Join));

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
                    this.m_client.Close();
                    break;
                }

                if (worker.CancellationPending) 
                    break;
                else
                    worker.ReportProgress(1, read);
            }

            worker.ReportProgress(1, ConnectionStates.Disconnected);
            System.Threading.Thread.Sleep(1000 * this.m_reconnectTimeout);
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
                case Command.Ok:
                    if (this.ConnectionState == ConnectionStates.Connected)
                    {
                        this.ConnectionState = ConnectionStates.Joined;
                        SetMyInfo();
                    }
                    break;
                case Command.Xml:
                    ArrayList contacts = ParseXml(cmd.Value);
                    switch (cmd.Cmd)
                    {
                        case Command.List:
                            if(ContactsList != null) ContactsList(contacts);
                            break;
                        case Command.Update:
                            if (ContactsUpdate != null) ContactsUpdate(contacts);
                            break;
                        case Command.Add:
                            if (ContactsAdd != null) ContactsAdd(contacts);
                            break;
                        case Command.Delete:
                            if (ContactsDelete != null) ContactsDelete(contacts);
                            break;
                    }
                    break;
            }
        }

        private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.m_reconnect)
                this.m_worker.RunWorkerAsync();
        }

        private ArrayList ParseXml(String xml)
        {
            ArrayList contactList = new ArrayList();
            try
            {
                XDocument contactXml = XDocument.Parse(@xml);
                foreach (XElement contact in contactXml.Element("contacts").Elements())
                {
                    contactList.Add(new Contact(contact.Attribute("ip").Value, contact.Attribute("name").Value,
                        contact.Attribute("status").Value));
                }
            }
            catch (Exception er)
            {
                System.Diagnostics.Debug.WriteLine(er.ToString());
            }
            return contactList;
        }
    }
}
