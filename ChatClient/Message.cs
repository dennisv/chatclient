using System;
using System.Windows.Controls;

namespace ChatClient
{
    public class Message
    {
        private String m_type;
        public String Type
        {
            get { return m_type; }
        }

        private String m_name;
        public String Name
        {
            get { return m_name; }
        }

        private TextBlock m_text;
        public TextBlock Text
        {
            get { return m_text; }
        }

        public Message(String type, String name, TextBlock text)
        {
            this.m_type = type;
            this.m_name = name;
            this.m_text = text;
            this.m_text.TextWrapping = System.Windows.TextWrapping.Wrap;
        }
    }
}
