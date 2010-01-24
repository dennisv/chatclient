using System;
using System.ComponentModel;

namespace Tcp.Chat
{
    public class Contact : INotifyPropertyChanged
    {
        private string address;
        private string nickname;
        private string status;

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

        public Contact() { }
        public Contact(String address, String nickname, String status)
        {
            this.address = address;
            this.nickname = nickname;
            this.status = status;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(String name)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}