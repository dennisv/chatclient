using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows;
using ChatClient;

namespace IrcChat
{
    class IrcClientProvider
    {
        private String m_host;
        private int m_port;

        private TcpClient m_client;
        private NetworkStream m_stream;

        public StreamReader m_reader;
        private StreamWriter m_writer;

        private IrcPinger m_pinger;

        private IrcWindow m_window;

        public String Host
        {
            get { return this.m_host; }
            set { this.m_host = value; }
        }

        public int Port
        {
            get { return this.m_port; }
            set { this.m_port = value; }
        }

        public IrcWindow Window
        {
            get { return this.m_window; }
            set { this.m_window = value; }
        }

        public void Connect()
        {
            this.m_client = new TcpClient(this.m_host, this.m_port);
            this.m_stream = this.m_client.GetStream();

            this.m_reader = new StreamReader(this.m_stream);
            this.m_writer = new StreamWriter(this.m_stream);

            this.m_pinger = new IrcPinger(this);
            this.m_pinger.Start();

            this.Send("USER Noobinat0r 0 * :Noobinator Test");
            this.Send("NICK Noobinat0r");
            this.Send("JOIN #testurdetest");
        }

        public void Process(String line)
        {
            this.m_window.ReceivedMessage(line);
        }

        public void Send(String message)
        {
            this.m_writer.WriteLine(message, 0, message.Length);
            this.m_writer.Flush();
        }

        ~IrcClientProvider()
        {
            this.m_client.Close();
        }
    }
}
