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
using IrcChat;
using System.ComponentModel;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for IrcWindow.xaml
    /// </summary>
    public partial class IrcWindow : Window
    {
        private InputDialog m_ConnectDialog;

        private IrcClientProvider m_client;

        private BackgroundWorker m_worker;

        public IrcWindow()
        {
            InitializeComponent();
        }

        private void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            this.m_ConnectDialog = new InputDialog(this, "Connect...", "Enter server address (127.0.0.1[:port]):", "Connect", ConnectDialog_Click);
            this.m_ConnectDialog.ShowDialog();
        }

        private void ConnectDialog_Click(object sender, RoutedEventArgs e)
        {
            String ipaddress = this.m_ConnectDialog.tbValue.Text;
            if (App.IsValidIP(ipaddress))
            {
                //sbLabel.Content = "Connecting to: " + ipaddress + " ...";
            }
            else
            {
                MessageBox.Show("Non valid server address given");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_worker = new BackgroundWorker();
            this.m_worker.WorkerReportsProgress = true;
            this.m_worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.m_worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.m_client = new IrcClientProvider();
            this.m_client.Host = "irc.freenode.net";
            this.m_client.Port = 6667;
            this.m_client.Window = this;
            this.m_client.Connect();

            while (!this.m_client.m_reader.EndOfStream)
            {
                if (((BackgroundWorker)sender).CancellationPending) return;
                else
                {
                    ((BackgroundWorker)sender).ReportProgress(1, this.m_client.m_reader.ReadLine());
                }
                //System.Threading.Thread.Sleep(250);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.m_client.Process(e.UserState.ToString());
        }

        private void Window_Closing(object sender, EventArgs e)
        {
            this.m_client = null;
        }

        public void ReceivedMessage(String message)
        {
            Paragraph para = new Paragraph();
            para.Inlines.Add(message);
            this.fldHistory.Blocks.Add(para);
            this.richTextBox1.ScrollToEnd();
        }
    }
}
