using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace EwfUtil
{
    public partial class Config : Form
    {
        Poller poller = Poller.GetCheckerObject();
        public Config()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.driveCombo.SelectedItem = poller.Drive;
            this.pollingCombo.SelectedItem = poller.Interval.ToString();
            this.memorySpinner.Value = poller.Threshold;
            this.startupCombo.SelectedItem = poller.Startup;
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings.Remove("drive");
            config.AppSettings.Settings.Add("drive", this.driveCombo.SelectedItem.ToString());
            poller.Drive = this.driveCombo.SelectedItem.ToString();

            config.AppSettings.Settings.Remove("pollingInterval");
            config.AppSettings.Settings.Add("pollingInterval", this.pollingCombo.SelectedItem.ToString());
            poller.Interval = Int32.Parse(this.pollingCombo.SelectedItem.ToString());

            config.AppSettings.Settings.Remove("memoryThreshold");
            config.AppSettings.Settings.Add("memoryThreshold", this.memorySpinner.Value.ToString());
            poller.Threshold = Int32.Parse(this.memorySpinner.Value.ToString());

            config.AppSettings.Settings.Remove("onStartup");
            config.AppSettings.Settings.Add("onStartup", this.startupCombo.SelectedItem.ToString());
            poller.Startup = this.startupCombo.SelectedItem.ToString();

            config.Save(ConfigurationSaveMode.Modified);

            poller.restartTimer();

            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}