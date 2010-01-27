using System;
using System.ComponentModel;

namespace ChatClient
{
    public class ContactItem : INotifyPropertyChanged
    {
        private string address;
        private string nickname;
        private string status;
        private string protocol;
        private MSNPSharp.Contact m_contact;

        public String Address
        {
            get
            {
                return this.address;
            }
            set
            {
                this.address = value;
            }
        }
        public String Nickname
        {
            get
            {
                return this.nickname;
            }
            set
            {
                if (value == this.nickname)
                    return;

                this.nickname = value;
                this.OnPropertyChanged("Nickname");
            }
        }
        public String Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if (value == this.status)
                    return;

                this.status = value;
                this.OnPropertyChanged("Status");
            }
        }
        public String Protocol
        {
            get
            {
                return this.protocol;
            }
            set
            {
                if (value == this.protocol)
                    return;

                this.protocol = value;
                this.OnPropertyChanged("Protocol");
            }
        }

        public MSNPSharp.Contact Contact
        {
            get { return this.m_contact; }
            set { this.m_contact = value; }
        }

        public ContactItem() { }
        public ContactItem(String address, String nickname, String status, String protocol)
        {
            this.address = address;
            this.nickname = nickname;
            this.status = status;
            this.protocol = protocol;
        }

        public ContactItem(String address, String nickname, String status, String protocol, MSNPSharp.Contact contact)
        {
            this.address = address;
            this.nickname = nickname;
            this.status = status;
            this.protocol = protocol;
            this.m_contact = contact;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(String name)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
