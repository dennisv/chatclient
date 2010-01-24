using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Tcp.Chat;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Annoy annoy;

        private InputDialog m_ConnectDialog;
        private InputDialog m_ChatConnectDialog;
        private InputDialog m_NicknameDialog;
        private InputDialog m_StatusDialog;

        private Contacts m_contacts;
        private Server m_server;

        private Hashtable m_windows;

        public static DependencyProperty ContactsCollectionProperty = DependencyProperty.Register("ContactsCollection", typeof(ContactCollection), typeof(MainWindow));
        public ContactCollection ContactsCollection
        {
            get { return (ContactCollection)GetValue(ContactsCollectionProperty); }
            set { SetValue(ContactsCollectionProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_windows = new Hashtable();
            this.ContactsCollection = new ContactCollection();

            this.m_contacts = new Contacts(Properties.Settings.Default.ServerIp, Properties.Settings.Default.ServerPort);
            this.m_contacts.Nickname = tbNickname.Text.ToString();
            this.m_contacts.Reconnect = Properties.Settings.Default.Reconnect;
            this.m_contacts.ReconnectTimeout = Properties.Settings.Default.ReconnectTimeout;
            this.m_contacts.ConnectionStateChanged += new Contacts.ConnectionStateHandler(Contacts_ConnectionStateChanged);
            this.m_contacts.ContactsList += new Contacts.ContactsHandler(Contacts_ContactsList);
            this.m_contacts.ContactsAdd += new Contacts.ContactsHandler(Contacts_ContactsAdd);
            this.m_contacts.ContactsUpdate += new Contacts.ContactsHandler(Contacts_ContactsUpdate);
            this.m_contacts.ContactsDelete += new Contacts.ContactsHandler(Contacts_ContactsDelete);
            this.m_contacts.Connect();

            this.m_server = new Server(Properties.Settings.Default.ClientPort);
            this.m_server.ConnectionAccepted += new Server.ConnectionAcceptedHandler(Server_ConnectionAccepted);
            this.m_server.Listen();
        }

        private int ContactIndex(String address)
        {
            for (int index = 0; index < this.ContactsCollection.Count(); index++)
            {
                if (this.ContactsCollection[index].Address == address)
                    return index;
            }
            return -1;
        }

        #region Contacts events
        private void Contacts_ConnectionStateChanged(Contacts.ConnectionStates connectionState)
        {
            sbImage.Width = 16;
            sbImage.Height = 16;

            String file = null;
            switch (connectionState)
            {
                case Contacts.ConnectionStates.Connecting:
                    file = "socket--arrow.png";
                    sbLabel.Content = "Connecting to " + this.m_contacts.Host;
                    break;
                case Contacts.ConnectionStates.Connected:
                    file = "socket--pencil.png";
                    sbLabel.Content = "Successfully connected at " + String.Format("{0:HH:mm}", DateTime.Now);
                    break;
                case Contacts.ConnectionStates.Joined:
                    file = "socket.png";
                    sbLabel.Content = "Successfully joined at " + String.Format("{0:HH:mm}", DateTime.Now);
                    break;
                case Contacts.ConnectionStates.Disconnected:
                default:
                    file = "socket--exclamation.png";
                    sbLabel.Content = "Disconnected at " + String.Format("{0:HH:mm}", DateTime.Now);
                    break;
            }
            sbImage.Visibility = Visibility.Visible;
            sbImage.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/" + file));
        }

        private void Contacts_ContactsList(ArrayList contacts)
        {
            this.ContactsCollection.Clear();
            foreach(Contact contact in contacts)
            {
                this.ContactsCollection.Add(contact);
            }
        }

        private void Contacts_ContactsAdd(ArrayList contacts)
        {
            foreach (Contact contact in contacts)
            {
                this.ContactsCollection.Add(contact);
            }
        }

        private void Contacts_ContactsUpdate(ArrayList contacts)
        {
            foreach (Contact contact in contacts)
            {
                Contact c = this.ContactsCollection[ContactIndex(contact.Address)];
                c.Nickname = contact.Nickname;
                c.Status = contact.Status;

                if (this.m_windows.Contains(contact.Address) && ((ChatWindow)this.m_windows[contact.Address]).Open)
                {
                    ChatWindow window = ((ChatWindow)this.m_windows[contact.Address]);
                    window.ContactNickname = contact.Nickname;
                    window.ContactStatus = contact.Status;
                }
            }
        }

        private void Contacts_ContactsDelete(ArrayList contacts)
        {
            foreach (Contact contact in contacts)
            {
                this.ContactsCollection.RemoveAt(ContactIndex(contact.Address));
            }
        }
        #endregion

        private void Server_ConnectionAccepted(System.Net.Sockets.TcpClient client)
        {
            String address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            if (!this.m_windows.Contains(address) || !((ChatWindow)this.m_windows[address]).Open)
            {
                ChatWindow window = new ChatWindow(client);

                int index = ContactIndex(address);
                if (index != -1)
                {
                    window.ContactNickname = this.ContactsCollection[index].Nickname;
                    window.ContactStatus = this.ContactsCollection[index].Status;
                }

                this.m_windows[address] = window;
                window.ShowActivated = false;
            }
            else
            {
                ChatWindow window = (ChatWindow)this.m_windows[address];
                window.Reconnect(client);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            this.m_ConnectDialog = new InputDialog(this, "Connect...", "Enter server address (127.0.0.1):", "Connect", ConnectDialog_Click);
            this.m_ConnectDialog.ShowDialog();
        }

        private void ConnectDialog_Click(object sender, RoutedEventArgs e)
        {
            String ipaddress = this.m_ConnectDialog.tbValue.Text;
            if (App.IsValidIP(ipaddress))
            {
                sbLabel.Content = "Connecting to: " + ipaddress + " ...";
            }
            else
            {
                MessageBox.Show("Non valid ip address given");
            }
        }

        private void Chat_Click(object sender, RoutedEventArgs e)
        {
            this.m_ChatConnectDialog = new InputDialog(this, "Connect...", "Enter client address (127.0.0.1):", "Chat", ChatConnectDialog_Click);
            this.m_ChatConnectDialog.ShowDialog();
        }

        private void ChatConnectDialog_Click(object sender, RoutedEventArgs e)
        {
            String ipaddress = this.m_ChatConnectDialog.tbValue.Text;
            if (App.IsValidIP(ipaddress))
            {
                ChatWindow window = new ChatWindow(ipaddress, Properties.Settings.Default.ClientPort);
                window.Show();
            }
            else
            {
                MessageBox.Show("Non valid ip address given");
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            this.annoy = new Annoy(tbNickname.Text.ToString());
            this.annoy.Changed += new Annoy.AnnoyHandler(annoy_Changed);
            this.annoy.Start();
        }

        void annoy_Changed(string name)
        {
            this.m_contacts.Nickname = name;
            tbNickname.Text = name;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lbContacts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbContacts.SelectedItem != null)
            {
                Contact selected = (Contact)lbContacts.SelectedItem;
                if (!this.m_windows.Contains(selected.Address) || !((ChatWindow)this.m_windows[selected.Address]).Open)
                {
                    ChatWindow window = new ChatWindow(selected.Address, Properties.Settings.Default.ClientPort);
                    window.Nickname = this.m_contacts.Nickname;
                    window.Nickname = this.m_contacts.Status;
                    window.ContactNickname = selected.Nickname;
                    window.ContactStatus = selected.Status;
                    this.m_windows[selected.Address] = window;
                    window.Show();
                    window.Activate();
                }
                else
                {
                    ((ChatWindow)this.m_windows[selected.Address]).Focus();
                }
            }
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            String status = ((MenuItem)sender).Header.ToString().ToLower();
            if (status == "custom...")
            {
                this.m_StatusDialog = new InputDialog(this, "Status...", "Enter new status message:", "Change", StatusDialog_Click);
                this.m_StatusDialog.ShowDialog();
                return;
            }
            this.m_contacts.Status = status;
        }

        private void StatusDialog_Click(object sender, RoutedEventArgs e)
        {
            String status = this.m_StatusDialog.tbValue.Text;
            if (status.Trim() == "")
                return;
            this.m_contacts.Status = status;
        }

        private void miChangeNickname_Click(object sender, RoutedEventArgs e)
        {
            if(this.annoy != null) this.annoy.Stop();
            this.m_NicknameDialog = new InputDialog(this, "Nickname...", "Enter new nickname:", "Change", NicknameDialog_Click);
            this.m_NicknameDialog.ShowDialog();
        }

        private void NicknameDialog_Click(object sender, RoutedEventArgs e)
        {
            String nickname = this.m_NicknameDialog.tbValue.Text;
            if (nickname.Trim() == "")
                return;
            tbNickname.Text = nickname;
            this.m_contacts.Nickname = nickname;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();

            this.m_contacts.Disconnect();
            this.m_server.Stop();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Configuration settings = new Configuration();
            settings.Owner = this;
            settings.ShowDialog();

            if (settings.DialogResult.HasValue && settings.DialogResult.Value)
            {
                Properties.Settings.Default.Save();

                if (Properties.Settings.Default.ServerIp != this.m_contacts.Host)
                    this.m_contacts.Host = Properties.Settings.Default.ServerIp;

                if (Properties.Settings.Default.ServerPort != this.m_contacts.Port)
                    this.m_contacts.Port = Properties.Settings.Default.ServerPort;

                if (Properties.Settings.Default.Reconnect != this.m_contacts.Reconnect)
                    this.m_contacts.Reconnect = Properties.Settings.Default.Reconnect;

                if (Properties.Settings.Default.ReconnectTimeout != this.m_contacts.ReconnectTimeout)
                    this.m_contacts.ReconnectTimeout = Properties.Settings.Default.ReconnectTimeout;
            }
        }
    }
}
