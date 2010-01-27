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
using System.Windows.Shapes;

using MSNPSharp;
using MSNPSharp.Core;
using System.Windows.Threading;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MsnChatWindow : Window
    {
        public static DependencyProperty MessageCollectionProperty = DependencyProperty.Register("MessageCollection", typeof(MessageCollection), typeof(MsnChatWindow));
        public MessageCollection MessageCollection
        {
            get { return (MessageCollection)GetValue(MessageCollectionProperty); }
            set { SetValue(MessageCollectionProperty, value); }
        }

        private MainWindow m_clientwindow;

        private Conversation m_conversation;
        public Conversation Conversation
        {
            get { return this.m_conversation; }
        }

        public MsnChatWindow(Conversation conversation, MainWindow clientwindow, Contact contact)
        {
            InitializeComponent();

            this.MessageCollection = new MessageCollection();
            this.MessageCollection.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(MessageCollection_CollectionChanged);

            if (contact == null && conversation.Contacts.Count > 0)
            {
                contact = conversation.Contacts[0];
            }

            this.m_conversation = conversation;
            AddEvent();

            this.m_clientwindow = clientwindow;

            if (contact != null)
            {
                this.m_conversation.Invite(contact);
            }
        }

        public void AttachConversation(Conversation convers)
        {
            Conversation.End();
            this.m_conversation = convers;   //Use the latest conversation, WLM just do the same.
            AddEvent();
        }

        public bool CanAttach(Conversation newconvers)
        {
            if (((Conversation.Type & ConversationType.Chat) == ConversationType.Chat) ||
                (this.IsVisible == true && Conversation.Type == ConversationType.SwitchBoard))
            {
                if ((newconvers.Type | ConversationType.Chat) == (Conversation.Type | ConversationType.Chat))
                {
                    if (Conversation.Contacts.Count == newconvers.Contacts.Count)
                    {
                        foreach (Contact ct in newconvers.Contacts)
                        {
                            if (!Conversation.Contacts.Contains(ct))
                                return false;
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        private void AddEvent()
        {
            Conversation.TextMessageReceived += new EventHandler<TextMessageEventArgs>(Conversation_TextMessageReceived);
        }

        private void RemoveEvent()
        {
            Conversation.TextMessageReceived -= new EventHandler<TextMessageEventArgs>(Conversation_TextMessageReceived);
        }

        void Conversation_TextMessageReceived(object sender, TextMessageEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new EventHandler<TextMessageEventArgs>(Conversation_TextMessageReceived), sender, e);
                return;
            }

            if (!this.IsVisible)
                Show();

            this.MessageCollection.Add(new Message("Message", e.Sender.Name, new TextBlock() { Text = e.Message.Text }));
        }

        void MessageCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            svMessages.ScrollToEnd();
        }

        private void tbMessage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Conversation.SendTypingMessage();

            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;

                if (this.Conversation.Expired)
                {
                    this.MessageCollection.Add(new Message("Notice", "", new TextBlock() { FontStyle = FontStyles.Italic, Text = "You are not connected" }));
                    return;
                }

                String message = tbMessage.Text;
                if (message == "")
                    return;

                this.Conversation.SendTextMessage(new TextMessage(message));

                this.MessageCollection.Add(new Message("Message", Conversation.Messenger.Owner.Name, new TextBlock() { Text = message }));

                tbMessage.Text = "";
            }
        }

        private void MsnChat_Closed(object sender, EventArgs e)
        {
            RemoveEvent();
            if (!Conversation.Ended)
            {
                Conversation.End();
            }

            this.m_clientwindow.ConversationWindows.Remove(this);
        }
    }
}
