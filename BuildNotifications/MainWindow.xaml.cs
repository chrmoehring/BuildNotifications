﻿using BuildNotifications.Resources.Window;
using BuildNotifications.ViewModel;

namespace BuildNotifications
{
    public partial class MainWindow : CustomWindow
    {
        public MainWindow()
        {
            DataContext = new MainViewModel();
            InitializeComponent();
        }
    }
}