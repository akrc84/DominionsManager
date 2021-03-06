﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DominionsManager
{
    public partial class DominionsManager : Form
    {
        StringCollection gameNames;
        public DominionsManager()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            gameNames = Properties.Settings.Default.GameName;

            ListGameNames();

            if (this.listBox1.Items.Count > 0)
                this.listBox1.SelectedItem = this.listBox1.Items[0];
        }

        //TODO make something better than shity list
        private void ListGameNames()
        {
            listBox1.Items.Clear();
            foreach (var gameName in gameNames)
            {
                listBox1.Items.Add(gameName);
            }
        }

        ///Retrives all turns from mail
        private void button2_Click(object sender, EventArgs e)
        {
            foreach (string gameName in gameNames)
            {
                var x = GmailClient.GetMail(gameName);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string input = "";
            ShowInputDialog(ref input);

            Properties.Settings.Default.GameName.Add(input);
            Properties.Settings.Default.Save();

            ListGameNames();
        }

        //get next turn only
        private void button5_Click(object sender, EventArgs e)
        {
            string gameName = this.listBox1.SelectedItem.ToString();
            GameTurnInfo gti = new GameTurnInfo(gameName);

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                List<GameTurnInfo> mailsomething = GmailClient.GetMail(gameName, gti.LastTurn + 1);

                if (mailsomething.Count == 0)
                {
                    MessageBox.Show(this, "No new turn available for " + gameName);
                    return;
                }

                mailsomething.Sort();

                var lastTurn = mailsomething.Last();
                lastTurn.SaveGameFoder(true);

                foreach (GameTurnInfo gameTurnInfo in mailsomething)
                {
                    gameTurnInfo.SaveGameFoder(false);
                }

                LoadGameTurns(gameName);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void btnRemoveGame_Click(object sender, EventArgs e)
        {
            string gameName = this.listBox1.SelectedItem.ToString();

            Properties.Settings.Default.GameName.Remove(gameName);
            Properties.Settings.Default.Save();

            ListGameNames();
        }


        private static DialogResult ShowInputDialog(ref string input)
        {
            System.Drawing.Size size = new System.Drawing.Size(200, 70);
            Form inputBox = new Form();

            inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            inputBox.ClientSize = size;
            inputBox.Text = "Name";

            System.Windows.Forms.TextBox textBox = new TextBox();
            textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
            textBox.Location = new System.Drawing.Point(5, 5);
            textBox.Text = input;
            inputBox.Controls.Add(textBox);

            Button okButton = new Button();
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.Text = "&OK";
            okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            input = textBox.Text;
            return result;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem == null)
                return;

            string x = listBox.SelectedItem.ToString();

            if (LoadGameTurns(x))
                RefreshWebGameStatus(x);

        }

        private void RefreshWebGameStatus(string x)
        {
            this.webBrowser1.Url = new System.Uri(string.Format(@"http://www.llamaserver.net/gameinfo.cgi?game={0}", x));
        }

        private bool LoadGameTurns(string gameName)
        {
            listBox2.Items.Clear();

            string targetDir = Path.Combine(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.SaveGameDir), gameName);

            if (!Directory.Exists(targetDir))
            {
                var x = MessageBox.Show("Game directory missing " + targetDir + ". Create directory?", "Directory not found", MessageBoxButtons.YesNoCancel);
                
                if (x == DialogResult.Yes)
                    System.IO.Directory.CreateDirectory(targetDir);
                else
                    return false;
            }

            List<GameTurnInfo> gameTurnInfos = new List<GameTurnInfo>();

            string[] fileEntries = System.IO.Directory.GetFiles(targetDir);

            foreach (string fileName in fileEntries)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                GameTurnInfo gameTurnInfo = new GameTurnInfo(gameName, fileInfo);

                if (gameTurnInfo.Turn > 0)
                    gameTurnInfos.Add(gameTurnInfo);
            }

            this.listBox2.Items.AddRange(gameTurnInfos.OrderByDescending(i => i).ToArray());

            return true;
        }
        private void ProcessSaveGameFile(FileInfo fileInfo, string gameName)
        {
            GameTurnInfo gameTurnInfo = new GameTurnInfo(gameName, fileInfo);
            listBox2.Items.Add(gameTurnInfo);
            //listBox2.Items.Add(fileInfo.Name);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string x = ((System.Windows.Forms.ListBox)listBox1).SelectedItem.ToString();

            SendGameTurn(x);
        }

        private void SendGameTurn(string x)
        {
            string gameName = this.listBox1.SelectedItem.ToString();

            GameTurnInfo gameTurnInfo = new GameTurnInfo(gameName);

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                GmailClient.SendMail(gameTurnInfo);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }


            MessageBox.Show(string.Format("Mail sent for game {0} turn {1}", gameTurnInfo.GameName, gameTurnInfo.LastTurn.ToString()));
        }

        private void btn_playDom4_Click(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = Properties.Settings.Default.Dom4exe;

            if (this.listBox1.SelectedItem != null)
            {
                startInfo.Arguments = this.listBox1.SelectedItem.ToString();
            }

            if (this.listBox2.SelectedItem.GetType() == typeof(GameTurnInfo))
            {
                GameTurnInfo x = this.listBox2.SelectedItem as GameTurnInfo;

                x.CurrentTurnFile.CopyTo(x.TurnFile.FullName, true);
            }

            Process.Start(startInfo);
        }
    }
}
