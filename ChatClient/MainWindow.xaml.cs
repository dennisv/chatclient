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
using System.Windows.Threading;

using Tcp.Chat;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net;

using MSNPSharp;
using MSNPSharp.Core;
using System.IO;

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

        private Messenger m_messenger;

        private Hashtable m_windows;

        public static DependencyProperty ContactsCollectionProperty = DependencyProperty.Register("ContactsCollection", typeof(ContactCollection), typeof(MainWindow));
        public ContactCollection ContactsCollection
        {
            get { return (ContactCollection)GetValue(ContactsCollectionProperty); }
            set { SetValue(ContactsCollectionProperty, value); }
        }

        private List<MsnChatWindow> m_conversationwindows = new List<MsnChatWindow>(0);
        public List<MsnChatWindow> ConversationWindows
        {
            get
            {
                return m_conversationwindows;
            }
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

            this.m_messenger = new Messenger();
            this.m_messenger.Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);
            this.m_messenger.Nameserver.SignedOff += new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff);
            this.m_messenger.Owner.DisplayImageChanged += new EventHandler<EventArgs>(Owner_DisplayImageChanged);
            this.m_messenger.ConversationCreated += new EventHandler<ConversationCreatedEventArgs>(m_messenger_ConversationCreated);
            this.m_messenger.ContactService.ReverseAdded += new EventHandler<ContactEventArgs>(ContactService_ReverseAdded);
        }

        void ContactService_ReverseAdded(object sender, ContactEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<ContactEventArgs>(ContactService_ReverseAdded), sender, e);
                return;
            }

            MSNPSharp.Contact contact = e.Contact;
            if (this.m_messenger.Nameserver.Owner.NotifyPrivacy == NotifyPrivacy.PromptOnAdd)
            {
                if (contact.OnPendingList ||
                    (contact.OnReverseList && !contact.OnAllowedList && !contact.OnBlockedList && !contact.OnPendingList))
                {
                    if (MessageBox.Show("Want to add " + contact.Name + " (" + contact.Mail + ") to your buddylist?", "Buddy invite", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        this.m_messenger.Nameserver.ContactService.AddNewContact(contact.Mail);
                        System.Threading.Thread.Sleep(200);
                        contact.OnPendingList = false;
                    }
                    else
                    {
                        contact.Blocked = true;
                    }
                    contact.OnPendingList = false;
                }
                else
                {
                    MessageBox.Show(contact.Mail + " accepted your invitation and added you their contact list.");
                }
            }
        }

        private delegate MsnChatWindow CreateConversationDelegate(Conversation conversation, MSNPSharp.Contact remote);
        private MsnChatWindow CreateConversationWindow(Conversation conversation, MSNPSharp.Contact remote)
        {
            foreach (MsnChatWindow cwindow in ConversationWindows)
            {
                if (cwindow.CanAttach(conversation))
                {
                    cwindow.AttachConversation(conversation);
                    return cwindow;
                }
            }

            MsnChatWindow chatWindow = new MsnChatWindow(conversation, this, remote);
            ConversationWindows.Add(chatWindow);
            return chatWindow;
        }

        void m_messenger_ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("conversation created!");
            if(e.Initiator == null)
                e.Conversation.ContactJoined += new EventHandler<ContactEventArgs>(Conversation_ContactJoined);
        }

        void Conversation_ContactJoined(object sender, ContactEventArgs e)
        {
            this.Dispatcher.Invoke(new CreateConversationDelegate(CreateConversationWindow), new object[] { sender, null });
            ((Conversation)sender).ContactJoined -= Conversation_ContactJoined;
        }

        void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff), sender, e);
                return;
            }

            for (int index = 0; index < this.ContactsCollection.Count(); index++)
            {
                if (this.ContactsCollection[index].Protocol == "msn")
                    this.ContactsCollection.Remove(this.ContactsCollection[index]);
            }
        }

        void Owner_DisplayImageChanged(object sender, EventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<EventArgs>(Owner_DisplayImageChanged), sender, e);
                return;
            }

            if (!Directory.Exists("displayimages"))
                Directory.CreateDirectory("displayimages");

            String filename = "displayimages/" + ((MSNPSharp.Contact)sender).Mail + ".png";
            this.m_messenger.Owner.DisplayImage.Image.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void Nameserver_SignedIn(object sender, EventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<EventArgs>(Nameserver_SignedIn), sender, e);
                return;
            }

            System.Media.SoundPlayer sound = new System.Media.SoundPlayer();
            sound.SoundLocation = @"C:\Windows\Media\tada.wav";
            sound.Play();

            this.UpdateContactList();
        }

        private String ContactToMsnName(MSNPSharp.Contact contact)
        {
            String name = contact.Name;
            if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
            {
                name += " - " + contact.PersonalMessage.Message;
            }
            if (contact.Name != contact.Mail)
            {
                name += " (" + contact.Mail + ")";
            }

            return name;
        }

        private void UpdateContactList()
        {
            foreach (MSNPSharp.Contact contact in this.m_messenger.ContactList.All)
            {
                contact.ContactOnline += new EventHandler<StatusChangedEventArgs>(contact_ContactOnline);
                contact.StatusChanged += new EventHandler<StatusChangedEventArgs>(contact_StatusChanged);
                contact.ScreenNameChanged += new EventHandler<EventArgs>(contact_ScreenNameChanged);
                contact.PersonalMessageChanged += new EventHandler<EventArgs>(contact_ScreenNameChanged);
                String name = ContactToMsnName(contact);
                this.ContactsCollection.Add(new ContactItem(contact.Mail, name, contact.Status.ToString(), "msn", contact));
            }
        }

        void contact_ScreenNameChanged(object sender, EventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<EventArgs>(contact_ScreenNameChanged), sender, e);
                return;
            }

            MSNPSharp.Contact contact = (MSNPSharp.Contact)sender;
            ContactItem c = this.ContactsCollection[ContactIndex(contact.Mail)];
            String name = ContactToMsnName(contact);
            c.Nickname = name;
        }

        private void contact_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<StatusChangedEventArgs>(contact_StatusChanged), sender, e);
                return;
            }

            MSNPSharp.Contact contact = (MSNPSharp.Contact)sender;
            ContactItem c = this.ContactsCollection[ContactIndex(contact.Mail)];
            c.Status = contact.Status.ToString();
        }

        private void contact_ContactOnline(object sender, StatusChangedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<StatusChangedEventArgs>(contact_ContactOnline), sender, e);
                return;
            }

            MSNPSharp.Contact contact = (MSNPSharp.Contact)sender;
            ContactItem c = this.ContactsCollection[ContactIndex(contact.Mail)];
            String name = ContactToMsnName(contact);
            c.Nickname = name;
            c.Status = contact.Status.ToString();
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
            foreach(Tcp.Chat.Contact contact in contacts)
            {
                this.ContactsCollection.Add(new ContactItem(contact.Address, contact.Nickname, contact.Status, "chat"));
            }
        }

        private void Contacts_ContactsAdd(ArrayList contacts)
        {
            foreach (Tcp.Chat.Contact contact in contacts)
            {
                this.ContactsCollection.Add(new ContactItem(contact.Address, contact.Nickname, contact.Status, "chat"));
            }
        }

        private void Contacts_ContactsUpdate(ArrayList contacts)
        {
            foreach (Tcp.Chat.Contact contact in contacts)
            {
                ContactItem c = this.ContactsCollection[ContactIndex(contact.Address)];
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
            foreach (Tcp.Chat.Contact contact in contacts)
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
                ContactItem selected = (ContactItem)lbContacts.SelectedItem;
                if (selected.Protocol == "msn")
                {
                    MSNPSharp.Contact contact = selected.Contact;

                    bool activate = false;
                    MsnChatWindow activeForm = null;
                    foreach (MsnChatWindow conv in ConversationWindows)
                    {
                        if (conv.Conversation.HasContact(contact) &&
                            (conv.Conversation.Type & ConversationType.Chat) == ConversationType.Chat)
                        {
                            activeForm = conv;
                            activate = true;
                        }

                    }

                    if (activate)
                    {
                        if (activeForm.WindowState == WindowState.Minimized)
                            activeForm.Show();

                        activeForm.Activate();
                        return;
                    }

                    Conversation convers = this.m_messenger.CreateConversation();
                    MsnChatWindow window = CreateConversationWindow(convers, contact);

                    window.Show();

                    return;
                }

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

        private void Msn_Click(object sender, RoutedEventArgs e)
        {
            MsnLoginWindow msn = new MsnLoginWindow(this.m_messenger);
            msn.Owner = this;
            msn.Show();
        }
    }
}
