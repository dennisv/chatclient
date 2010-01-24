using System;
using System.Threading;

namespace IrcChat
{
    class IrcPinger
    {
        private IrcClientProvider m_client;
        private Thread m_thread;

        public IrcPinger(IrcClientProvider client)
        {
            this.m_client = client;
            this.m_thread = new Thread(new ThreadStart(this.Run));
        }

        public void Start()
        {
            this.m_thread.Start();
        }

        public void Run()
        {
            this.m_client.Send("PING :" + this.m_client.Host);
            Thread.Sleep(15000);
        }
    }
}
