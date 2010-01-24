using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Sockets;

using Tcp.Chat;
using System.Collections;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private Client m_client;

        private bool m_active;
        private bool m_flashing;
        private bool m_started;
        
        private bool m_open;
        public bool Open
        {
            get { return this.m_open; }
        }

        private bool m_inContactList;
        public bool InContactList
        {
            set { this.m_inContactList = value; }
            get { return this.m_inContactList; }
        }

        private String m_contactNickname;
        public String ContactNickname
        {
            set
            {
                this.m_contactNickname = value;
                ContactInfoChanged();
            }
            get { return this.m_contactNickname; }
        }

        private String m_contactStatus;
        public String ContactStatus
        {
            set
            {
                this.m_contactStatus = value;
                ContactInfoChanged();
            }
            get { return this.m_contactStatus; }
        }

        private String m_nickname;
        public String Nickname
        {
            set
            {
                this.m_nickname = value;
                MyInfoChanged();
            }
            get { return this.m_contactNickname; }
        }

        private String m_status;
        public String Status
        {
            set
            {
                this.m_status = value;
                MyInfoChanged();
            }
            get { return this.m_contactStatus; }
        }

        private Dictionary<String, String> m_smileys;

        public static DependencyProperty MessageCollectionProperty = DependencyProperty.Register("MessageCollection", typeof(MessageCollection), typeof(ChatWindow));
        public MessageCollection MessageCollection
        {
            get { return (MessageCollection)GetValue(MessageCollectionProperty); }
            set { SetValue(MessageCollectionProperty, value); }
        }

        public ChatWindow(TcpClient client)
        {
            this.m_started = false;
            this.m_client = new Client(client);
            this.m_active = false;
            Initialize();
            InitializeComponent();
        }

        public ChatWindow(String address, int port)
        {
            this.m_started = true;
            this.m_client = new Client(address, port);
            this.m_active = true;
            Initialize();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_open = true;
        }

        private void Initialize()
        {
            this.MessageCollection = new MessageCollection();
            this.MessageCollection.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(MessageCollection_CollectionChanged);
            this.m_smileys = new Dictionary<String, String>();
            #region smileys
            this.m_smileys.Add(":)", "smiley.png");
            this.m_smileys.Add(":(", "smiley-sad.png");
            this.m_smileys.Add(":'(", "smiley-cry.png");
            this.m_smileys.Add(":D", "smiley-grin.png");
            this.m_smileys.Add(":P", "smiley-razz.png");
            this.m_smileys.Add(":$", "smiley-red.png");
            this.m_smileys.Add(":|", "smiley-eek.png");
            this.m_smileys.Add(";)", "smiley-wink.png");
            this.m_smileys.Add("B)", "smiley-cool.png");
            #endregion

            this.m_contactNickname = "No Name";
            this.m_contactStatus = "Online";

            this.m_client.ConnectionStateChanged += new Client.ConnectionStateHandler(Client_ConnectionStateChanged);
            this.m_client.ContactStatusChanged += new Client.ContactStatusChangedHandler(Client_ContactStatusChanged);
            this.m_client.ContactNicknameChanged += new Client.ContactNicknameChangedHandler(Client_ContactNicknameChanged);
            this.m_client.MessageReceived += new Client.MessageReceivedHandler(Client_MessageReceived);
            this.m_client.Connect();
        }

        void MessageCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            svMessages.ScrollToEnd();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.m_active = true;
            FlashWindow.Stop(this);
            this.m_flashing = false;
            tbMessage.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.m_active = false;
        }

        private TextBlock ReplaceSmileys(String original)
        {
            char sub = (char)26;
            ArrayList tmp = new ArrayList();
            foreach (String smiley in this.m_smileys.Keys.ToArray())
            {
                tmp.Add(Regex.Escape(smiley));
            }
            String subs = Regex.Replace(original,
                "(" + String.Join("|", (String[])tmp.ToArray(typeof(String))) + ")",
                delegate(Match m) { return sub + this.m_smileys[m.Value.ToUpper()] + sub; },
                RegexOptions.IgnoreCase
            );
            String[] pieces = subs.Split(sub);
            TextBlock textblock = new TextBlock();
            foreach (String piece in pieces)
            {
                if (this.m_smileys.ContainsValue(piece))
                {
                    BitmapImage bitmap = new BitmapImage(new Uri("pack://application:,,,/Icons/" + piece));
                    Image image = new Image();
                    image.Source = bitmap;
                    image.Width = 16;
                    image.Height = 16;
                    textblock.Inlines.Add(image);
                }
                else
                {
                    textblock.Inlines.Add(piece);
                }
            }
            return textblock;
        }

        private void ContactInfoChanged()
        {
            this.Title = this.m_contactNickname + " (" + this.m_contactStatus + ")";
        }

        private void MyInfoChanged()
        {
            this.m_client.Nickname = this.m_nickname;
            this.m_client.Status = this.m_status;
        }

        public void Reconnect(TcpClient client)
        {
            this.m_client.AttachClient(client, false);
            if (!this.m_started)
                Show();
        }

        public void Reconnect(TcpClient client, bool connect)
        {
            this.m_client.AttachClient(client, connect);
            if (!this.m_started)
                Show();
        }

        private void Client_ConnectionStateChanged(Client.ConnectionStates connectionState)
        {
            sbImage.Width = 16;
            sbImage.Height = 16;

            String file = null;
            switch (connectionState)
            {
                case Client.ConnectionStates.Connecting:
                    file = "socket--arrow.png";
                    sbLabel.Content = "Connecting to " + this.m_contactNickname;
                    break;
                case Client.ConnectionStates.Connected:
                    file = "socket.png";
                    sbLabel.Content = "Successfully connected at " + String.Format("{0:HH:mm}", DateTime.Now);
                    break;
                case Client.ConnectionStates.Disconnected:
                    file = "socket--exclamation.png";
                    sbLabel.Content = "Disconnected at " + String.Format("{0:HH:mm}", DateTime.Now);

                    this.MessageCollection.Add(new Message("Notice", "", new TextBlock() { FontStyle = FontStyles.Italic, Text = this.m_contactNickname + " disconnected" }));
                    break;
            }
            sbImage.Visibility = Visibility.Visible;
            sbImage.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/" + file));
        }

        private void Client_ContactStatusChanged(String status)
        {
            this.ContactStatus = status;
        }

        private void Client_ContactNicknameChanged(String name)
        {
            this.ContactNickname = name;
        }

        private void Client_MessageReceived(String message)
        {
            if (!this.m_started)
            {
                ShowActivated = false;
                Show();
            }

            message.Trim();

            this.MessageCollection.Add(new Message("Message", this.m_contactNickname, ReplaceSmileys(message)));

            sbImage.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/balloon-small.png"));
            sbLabel.Content = "Last message received at " + String.Format("{0:HH:mm}", DateTime.Now);

            if (!this.m_active)
            {
                System.Media.SoundPlayer sound = new System.Media.SoundPlayer();
                sound.SoundLocation = @"C:\Windows\Media\Speech On.wav";
                sound.Play();
            }
            if (!this.m_active && !this.m_flashing)
            {
                FlashWindow.Flash(this);
                this.m_flashing = true;
            }
        }

        private void tbMessage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;

                if (this.m_client.ConnectionState != Client.ConnectionStates.Connected)
                {
                    this.MessageCollection.Add(new Message("Notice", "", new TextBlock() { FontStyle = FontStyles.Italic, Text = "You are not connected" }));
                    return;
                    //this.m_client.Connect(true);
                }

                String message = tbMessage.Text;
                if (message == "")
                    return;

                if (!this.m_client.SendMessage(message))
                    return;

                this.MessageCollection.Add(new Message("Message", "You", ReplaceSmileys(message)));

                tbMessage.Text = "";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.m_client.Disconnect();
            this.m_open = false;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
                Close();
        }
    }
}
