﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uTorrentNotifier
{
    public class ClassRegistry : IDisposable
    {
        private WebUIAPI _webuiapi;
        private Config _config;
        private Prowl _prowl;
        private Growl _growl;
        private Twitter _twitter;
        private Boxcar _boxcar;

        public ClassRegistry()
        {
            this._config      = new Config();
            this._webuiapi    = new WebUIAPI(this._config);
            this._prowl       = new Prowl(this._config.Prowl);
            this._growl       = new Growl(this._config.Growl);
            this._twitter     = new Twitter(this._config.Twitter);
            this._boxcar      = new Boxcar(this._config.Boxcar);
        }

        public Config Config
        {
            get { return this._config; }
            set { this._config = value; }
        }
        public WebUIAPI uTorrent
        {
            get { return this._webuiapi; }
        }

        public Prowl Prowl
        {
            get { return this._prowl; }
        }

        public Growl Growl
        {
            get { return this._growl; }
        }

        public Twitter Twitter
        {
            get { return this._twitter; }
        }

        public Boxcar Boxcar
        {
            get { return this._boxcar; }
        }

        public static Version Version
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._webuiapi.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
