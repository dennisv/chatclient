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

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        RoutedEventHandler m_handler;

        public InputDialog(Window owner, String title, String label, String button, RoutedEventHandler handler)
        {
            InitializeComponent();

            this.Owner = owner;
            this.Title = title;
            this.lblDescription.Content = label;
            this.btnOk.Content = button;
            this.btnOk.Click += handler;
            this.m_handler = handler;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.tbValue.Focus();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void tbValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.m_handler(sender, e);
                this.Close();
            }
        }
    }
}
