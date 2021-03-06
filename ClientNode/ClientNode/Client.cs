﻿using networkLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ClientNode
{
    class Client
    {
        private transportClient client;
        public string networkController;
        private Config conf;
        private Grid chat;
        private TextBlock status;
        private int messageNumber = 0;
        private int rIndex;
        private MainWindow mainWindow;
        public string name { get; set; }
        public string nodeName { get; set; }
        public string CloudIP { get; set; }
        public string CloudPort { get; set; }
        public List<string> portsIn { get; set; }
        public List<string> portsOut { get; set; }

        private transportClient.NewMsgHandler messageHandler { get; set; }
        private transportClient.NewSignalization signHandler { get; set; }

        public Client(Grid chat, TextBlock status, MainWindow mainWindow)
        {
            this.chat = chat;
            this.status = status;
            this.mainWindow = mainWindow;
            this.chat = chat;
            this.status = status;
            rIndex = Grid.GetRow(chat);
        }

        public void readConfig(string path)
        {
            try
            {
                conf = new Config(path, Constants.Client);
                this.nodeName = conf.config[0];
                this.CloudIP = conf.config[1];
                this.CloudPort = conf.config[2];
                this.name = conf.config[3];
                this.networkController = conf.config[4];
                this.portsIn = conf.portsIn;
                this.portsOut = conf.portsOut;

                mainWindow.Title = "Client " + this.name;
                displayStatusMessage(Constants.CONFIG_OK, Constants.LOG_INFO);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                displayStatusMessage(Constants.CONFIG_ERROR + "...", Constants.LOG_ERROR);
                displayStatusMessage(" from " + path + "...", Constants.LOG_ERROR);
            }
        }
        private void newSignalization(object a, MessageArgs e)
        {
            displayStatusMessage(e.message, Constants.LOG_ERROR);
            this.mainWindow.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    this.mainWindow.ConnectButton.IsEnabled = true;
                    this.mainWindow.statusBar.Text = "Disonnected";
                    this.mainWindow.Button_1.IsEnabled = false;
                    this.mainWindow.statusBar.Foreground = Brushes.Red;
                })
            );
        }
        private void newMessageRecived(object a, MessageArgs e)
        {
            string message;
            try
            {
                //displayStatusMessage(e.message, Constants.LOG_ERROR);
                message = e.message.Split('&')[1];
                string dispMessage = message.Split('^')[1];
                addChatMessage(dispMessage, Constants.RIGHT);
            }
            catch
            {
                addChatMessage(e.message, Constants.RIGHT);
            }
        }

        #region serverOptions
        public void startService()
        {
            try
            {
                messageHandler = new transportClient.NewMsgHandler(newMessageRecived);
                signHandler = new transportClient.NewSignalization(newSignalization);
                client = new transportClient(CloudIP, CloudPort);
                client.OnNewMessageRecived += messageHandler;
                client.OnNewSignalization += signHandler;
                client.sendMessage(nodeName + "#");
                displayStatusMessage(Constants.SERVICE_START_OK, Constants.LOG_INFO);
            }

            catch
            {
                displayStatusMessage(Constants.SERVICE_START_ERROR, Constants.LOG_ERROR);
            }

            if (this.isStarted())
            {
                this.mainWindow.ConnectButton.IsEnabled = false;
                this.mainWindow.statusBar.Text = "Connected";
                this.mainWindow.Button_1.IsEnabled = true;
                this.mainWindow.statusBar.Foreground = Brushes.Green;
            }
        }

        public void stopService()
        {
            try
            {
                client.OnNewMessageRecived -= messageHandler;
                client.OnNewSignalization -= signHandler;
                messageHandler = null;
                signHandler = null;
                client.stopService();
                client = null;
            }
            catch
            {

            }
        }

        public void sendMessage(string msg, int port)
        {
            try
            {
                client.sendMessage(this.portsOut[port] + "&" + msg + "/");
                addChatMessage(msg, Constants.LEFT);
            }
            catch { }
        }

        public bool isStarted()
        {
            if (client != null)
                return client.isConnected();
            else
                return false;
        }
        #endregion

        #region Frontend
        private void displayStatusMessage(string message, int type)
        {
            var color = Brushes.Black;

            switch (type)
            {
                case networkLibrary.Constants.LOG_ERROR:
                    color = Brushes.Red;
                    break;
                case networkLibrary.Constants.LOG_INFO:
                    color = Brushes.Black;
                    break;
            }

            this.status.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    status.Text = message;
                    status.Foreground = color;
                })
            );

        }

        private void addChatMessage(string msg, int aligment)
        {
            var algmnt = System.Windows.TextAlignment.Left;
            var color = Brushes.Black;
            switch (aligment)
            {   
                case Constants.LEFT:
                    algmnt = System.Windows.TextAlignment.Left;
                    break;
                case Constants.RIGHT:
                    algmnt = System.Windows.TextAlignment.Right;
                    color = Brushes.Blue;
                    break;
            }

            this.chat.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    var t = new TextBlock();
                    t.Text = ("[" + DateTime.Now.ToString("HH:mm:ss") + "]" + Environment.NewLine +
                        msg + Environment.NewLine + Environment.NewLine);
                    t.TextAlignment = algmnt;
                    t.Foreground = color;
                    RowDefinition gridRow = new RowDefinition();
                    gridRow.Height = new GridLength(35);
                    chat.RowDefinitions.Add(gridRow);
                    Grid.SetRow(t, messageNumber);
                    messageNumber++;
                    chat.Children.Add(t);
                })
            );
        }
        #endregion
    }
}
