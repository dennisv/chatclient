using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

using Tcp.Chat;
using System.ComponentModel;
using System.Collections.Specialized;

namespace ChatClient
{
    public class ContactCollection : ObservableCollection<Contact>
    {
        protected override void InsertItem(int index, Contact item)
        {
            base.InsertItem(index, item);
            item.PropertyChanged += Item_PropertyChanged;
        }

        protected override void RemoveItem(int index)
        {
            this.Items[index].PropertyChanged -= Item_PropertyChanged;
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (Contact con in this.Items)
                con.PropertyChanged -= Item_PropertyChanged;

            base.ClearItems();
        }

        protected override void SetItem(int index, Contact item)
        {
            this.Items[index].PropertyChanged -= Item_PropertyChanged;
            base.SetItem(index, item);
            item.PropertyChanged += Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Contact item = (Contact)sender;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, this.IndexOf(item)));
        }  
    }
}
