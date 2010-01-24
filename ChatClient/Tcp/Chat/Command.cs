using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tcp.Chat
{
    class Command
    {
        #region types
        public const String Ok = "OK";
        public const String Error = "ERR";

        public const String Set = "SET";
        public const String Get = "GET";

        public const String Request = "REQ";
        public const String Send = "SND";
        public const String Xml = "XML";
        #endregion

        #region commands
        public const String Join = "JOIN";
        public const String Leave = "LEAVE";
        public const String Message = "MSG";

        public const String Version = "VERSION";
        public const String Status = "STATUS";
        public const String Nickname = "NICKNAME";

        public const String List = "LST";
        public const String Update = "UPD";
        public const String Add = "ADD";
        public const String Delete = "DEL";
        #endregion

        private String m_type;
        public String Type
        {
            get { return this.m_type; }
        }

        private String m_command;
        public String Cmd
        {
            get { return this.m_command; }
        }

        private String m_value;
        public string Value
        {
            get { return this.m_value; }
        }

        public Command(String message)
        {
            this.m_type = "";
            this.m_command = "";
            this.m_value = "";

            Parse(message);
        }

        private void Parse(String message)
        {
            String[] words = message.Split(' ');
            int count = words.Length;
            if (count >= 1)
                this.m_type = words[0];
            if (count >= 2)
                this.m_command = words[1];
            if (count >= 3)
                this.m_value = (message.Split(new char[] { ' ' }, 3))[2];
        }

        static public String Create(String type, String command)
        {
            return type + " " + command;
        }

        static public String Create(String type, String command, String value)
        {
            return type + " " + command + " " + value;
        }
    }
}
