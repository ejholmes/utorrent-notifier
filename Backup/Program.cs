﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly:CLSCompliant(true)]
namespace uTorrentNotifier
{
    public class Program : Form
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Program());
        }

        private NotifyIcon _trayIcon;
        private ContextMenu _trayMenu;
        private MenuItem _miStartAll;
        private MenuItem _miPauseAll;
        private MenuItem _menuItem2;
        private MenuItem _miSettings;
        private MenuItem _menuItem1;
        private MenuItem _miClose;

        private SettingsForm settingsForm;

        private ClassRegistry ClassRegistry;

        public Program()
        {
            bool firstRun = false;
            InitializeComponent();

            if (Properties.Settings.Default.FirstRun)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Reload();
                Properties.Settings.Default.FirstRun = false;
                firstRun = true;
            }
            this.ClassRegistry = new ClassRegistry();
            this.settingsForm = new SettingsForm(this.ClassRegistry);

            if (this.ClassRegistry.Config.CheckForUpdates)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(this.CheckForUpdates);
            }
            this.ClassRegistry.uTorrent.TorrentAdded += new WebUIAPI.TorrentAddedEventHandler(this.utorrent_TorrentAdded);
            this.ClassRegistry.uTorrent.DownloadComplete += new WebUIAPI.DownloadFinishedEventHandler(this.utorrent_DownloadComplete);
            this.ClassRegistry.uTorrent.WebUIError += new WebUIAPI.WebUIErrorEventHandler(this.utorrent_LogOnError);
            this.ClassRegistry.uTorrent.UpdatedList += new WebUIAPI.UpdatedListEventHandler(this.uTorrent_UpdatedList);
            this.ClassRegistry.uTorrent.Start();

            if (firstRun)
                this.settingsForm.Show();
        }

        void utorrent_LogOnError(object sender, Exception e)
        {
            if (!this.ClassRegistry.Config.Growl.Enable)
            {
                this._trayIcon.ShowBalloonTip(5000, "Login Error", e.Message, ToolTipIcon.Error);
                this._trayIcon.Text = global::uTorrentNotifier.Properties.Resources.Name + "\n" +
                    "Error connecting to uTorrent";
            }
            else
            {
                this.ClassRegistry.Growl.Add(GrowlNotificationType.Error, e.Message);
            }
        }

        void utorrent_DownloadComplete(List<TorrentFile> finished)
        {
            if (this.ClassRegistry.Config.Notifications.DownloadComplete)
            {
                foreach (TorrentFile f in finished)
                {
                    if (this.ClassRegistry.Config.Prowl.Enable)
                    {
                        this.ClassRegistry.Prowl.Add("Download Complete", f.Name);
                    }

                    if (this.ClassRegistry.Config.Growl.Enable)
                    {
                        this.ClassRegistry.Growl.Add(GrowlNotificationType.InfoComplete, f.Name);
                    }

                    if (this.ClassRegistry.Config.Twitter.Enable)
                    {
                        this.ClassRegistry.Twitter.Update("Downloaded " + f.Name);
                    }

                    if (this.ClassRegistry.Config.Boxcar.Enable)
                    {
                        this.ClassRegistry.Boxcar.Add("Download Complete: " + f.Name);
                    }

                    if (this.ClassRegistry.Config.ShowBalloonTips)
                    {
                        this._trayIcon.ShowBalloonTip(5000, "Download Complete", f.Name, ToolTipIcon.Info);
                    }
                }
            }
        }

        void utorrent_TorrentAdded(List<TorrentFile> added)
        {
            if (this.ClassRegistry.Config.Notifications.TorrentAdded)
            {
                foreach (TorrentFile f in added)
                {
                    if (this.ClassRegistry.Config.Prowl.Enable)
                    {
                        this.ClassRegistry.Prowl.Add("Torrent Added", f.Name);
                    }

                    if (this.ClassRegistry.Config.Growl.Enable)
                    {
                        this.ClassRegistry.Growl.Add(GrowlNotificationType.InfoAdded, f.Name);
                    }

                    if (this.ClassRegistry.Config.Twitter.Enable)
                    {
                        this.ClassRegistry.Twitter.Update("Added " + f.Name + " | " + Utilities.FormatBytes((long)f.Size));
                    }

                    if (this.ClassRegistry.Config.Boxcar.Enable)
                    {
                        this.ClassRegistry.Boxcar.Add("Torrent Added: " + f.Name);
                    }

                    if (this.ClassRegistry.Config.ShowBalloonTips)
                    {
                        this._trayIcon.ShowBalloonTip(5000, "Torrent Added", f.Name, ToolTipIcon.Info);
                    }
                }
            }
        }

        void uTorrent_UpdatedList(List<TorrentFile> torrents)
        {
            int downloading = torrents.Count(n => n.StatusString == WebUIAPI.StatusString.Downloading);
            if (downloading == 1)
            {
                this._trayIcon.Text = global::uTorrentNotifier.Properties.Resources.Name + "\n" +
                    downloading.ToString(CultureInfo.CurrentCulture) + " torrent downloading\n";
            }
            else
            {
                this._trayIcon.Text = global::uTorrentNotifier.Properties.Resources.Name + "\n" +
                    downloading.ToString(CultureInfo.CurrentCulture) + " torrents downloading\n";
            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            this.settingsForm.Show();
        }

        private void PauseAll_Click(object sender, EventArgs e)
        {
            this.ClassRegistry.uTorrent.PauseAll();
        }

        private void StartAll_Click(object sender, EventArgs e)
        {
            this.ClassRegistry.uTorrent.StartAll();
        }

        private void CheckForUpdates(object sender)
        {
            System.Net.WebClient webclient = new System.Net.WebClient();
            string latestVersion = webclient.DownloadString(Config.LatestVersion);

            Version latest = new Version(latestVersion);

            if (latest > ClassRegistry.Version)
            {
                if (MessageBox.Show("You are using version " + ClassRegistry.Version.ToString() + ". Would you like to download version " + latestVersion + "?",
                    "New Version Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                {
                    Process.Start(Config.LatestDownload);
                    Application.Exit();
                }
            }
        }

        private void InitializeComponent()
        {
            // 
            // StartAll
            // 
            this._miStartAll = new System.Windows.Forms.MenuItem();
            this._miStartAll.Index = 0;
            this._miStartAll.Text = "Start All";
            this._miStartAll.Click += new System.EventHandler(this.StartAll_Click);
            // 
            // PauseAll
            // 
            this._miPauseAll = new System.Windows.Forms.MenuItem();
            this._miPauseAll.Index = 1;
            this._miPauseAll.Text = "Pause All";
            this._miPauseAll.Click += new System.EventHandler(this.PauseAll_Click);
            // 
            // menuItem2
            // 
            this._menuItem2 = new System.Windows.Forms.MenuItem();
            this._menuItem2.Index = 2;
            this._menuItem2.Text = "-";
            // 
            // Settings
            // 
            this._miSettings = new System.Windows.Forms.MenuItem();
            this._miSettings.Index = 3;
            this._miSettings.Text = "Settings";
            this._miSettings.Click += new System.EventHandler(this.Settings_Click);
            // 
            // menuItem1
            // 
            this._menuItem1 = new System.Windows.Forms.MenuItem();
            this._menuItem1.Index = 4;
            this._menuItem1.Text = "-";
            // 
            // Close
            // 
            this._miClose = new System.Windows.Forms.MenuItem();
            this._miClose.Index = 5;
            this._miClose.Text = "Exit";
            this._miClose.Click += new System.EventHandler(this.Close_Click);

            this._trayMenu = new ContextMenu();
            this._trayMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                this._miStartAll,
                this._miPauseAll,
                this._menuItem2,
                this._miSettings,
                this._menuItem1,
                this._miClose});

            this._trayIcon = new NotifyIcon();
            this._trayIcon.Text = global::uTorrentNotifier.Properties.Resources.Name;
            this._trayIcon.Icon = global::uTorrentNotifier.Properties.Resources.un_icon_systray;
            this.Icon = global::uTorrentNotifier.Properties.Resources.un_icon;

            // Add menu to tray icon and show it.
            this._trayIcon.ContextMenu = this._trayMenu;
            this._trayIcon.Visible = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release the icon resource.
                this._trayIcon.Dispose();
                this.ClassRegistry.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}