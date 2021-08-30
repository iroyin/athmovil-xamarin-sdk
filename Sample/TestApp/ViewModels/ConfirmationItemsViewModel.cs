﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using ATHMovil.Purchase.Model;

namespace TestApp.ViewModels
{
    public class ConfirmationItemsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<PurchaseItem> Items { get; set; }

        public ConfirmationItemsViewModel()
        {
        }

        public ConfirmationItemsViewModel(List<PurchaseItem> items)
        {

            Items = new ObservableCollection<PurchaseItem>();

            Task.Run(async () =>
            {
                await Task.Delay(50);

                foreach (PurchaseItem item in items)
                {
                    Items.Add(item);
                }
            });
        }
    }
}
