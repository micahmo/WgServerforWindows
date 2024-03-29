﻿using System;
using System.Windows;
using WgServerforWindows.Models;

namespace WgServerforWindows.Controls
{
    /// <summary>
    /// Interaction logic for ServerStatusWindow.xaml
    /// </summary>
    public partial class ServerStatusWindow : Window
    {
        public ServerStatusWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            AppSettings.Instance.Tracker.Track(this);
        }
    }
}
