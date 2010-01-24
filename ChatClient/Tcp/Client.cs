using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.IO;

namespace Tcp
{
    public class Client
    {
        protected TcpClient m_client;
        protected NetworkStream m_stream;

        protected virtual void Send(String message)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            this.m_stream.Write(encoding.GetBytes(message), 0, message.Length);
            this.m_stream.Flush();
        }
    }
}
