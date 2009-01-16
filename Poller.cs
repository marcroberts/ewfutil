using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Configuration;

/**********************************Simple Tray Icon sample DOTNET 2.0***********************************************
 * This class creates the notification icon that dotnet 2.0 offers.
 * It will be displaying the status of the application with appropiate icons.
 * It will have a contextmenu that enables the user to open the form or exit the application.
 * The form could be used to change settings of the app which in turn are saved in the app.config or some other file.
 * This formless, useless, notification sample does only chane the icon and balloontext.
 * NOTE:Chacker is a Singleton class so it will only allow to be instantiated once, and therefore only one instance.
 * I have done this to prevent more then one icon on the tray and to share data with the form (if any)
 *
 ******************************************************************************************************************/

namespace EwfUtil
{
    class Poller : IDisposable
    {
        //Checker is a singleton
        private static readonly Poller checker = new Poller();

        String drive = ConfigurationManager.AppSettings["drive"];
        int threshold = Int32.Parse(ConfigurationManager.AppSettings["memoryThreshold"]);
        String startup = ConfigurationManager.AppSettings["onStartup"];
        bool enabled = false;

        String boot_cmd;
        int usage;

        ToolStripMenuItem discardMenuItem;
        ToolStripMenuItem commitMenuItem;
        ToolStripMenuItem disableMenuItem;
        ToolStripMenuItem enableMenuItem;

        //timer
        bool IsDisposing = false;
        int timeout = Int32.Parse(ConfigurationManager.AppSettings["pollingInterval"]); //30 sec 
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        //notify icon: prepare the icons we may use in the notification
        NotifyIcon notify;
        System.Drawing.Icon iconok = System.Drawing.Icon.FromHandle((new System.Drawing.Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("EwfUtil.Images.drive.png"))).GetHicon());
        System.Drawing.Icon iconwarn = System.Drawing.Icon.FromHandle((new System.Drawing.Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("EwfUtil.Images.drive_error.png"))).GetHicon());
        System.Drawing.Icon iconoff = System.Drawing.Icon.FromHandle((new System.Drawing.Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("EwfUtil.Images.drive_off.png"))).GetHicon());

        //GUI: the form is not loaded into memory before it used, after use it is removed from memory
        //The notification icon has a contextmenu
        Config form;
        bool formPresent=false;
        ContextMenuStrip contextmenu = new ContextMenuStrip();

        /**************************** Singleton *****************************************************************
         * Make the constructor private and create a public method that returns the object reference.
         * The method must be static to be able to be called from different classes at any given moment.
         * The object is created when the first reference is ask for.
         * After that no more instances are created.
         ********************************************************************************************************/
        
        public static Poller GetCheckerObject()
        {
            return checker;
        }

        public int Interval
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }

        public String Drive
        {
            get
            {
                return this.drive;
            }
            set
            {
                this.drive = value;
            }
        }

        public int Threshold
        {
            get
            {
                return this.threshold;
            }
            set
            {
                this.threshold = value;
            }
        }

        public String Startup
        {
            get
            {
                return this.startup;
            }
            set
            {
                this.startup = value;
            }
        }
        
        private Poller() //singleton so private constructor!
        {
            //create menu
            //popup contextmenu when doubleclicked or when clicked in the menu 

            enableMenuItem = new ToolStripMenuItem("Enable on Reboot");
            enableMenuItem.Click += new EventHandler(notify_setEnable);
            contextmenu.Items.Add(enableMenuItem);

            discardMenuItem = new ToolStripMenuItem("Discard on Reboot");
            discardMenuItem.Click += new EventHandler(notify_setDiscard);
            contextmenu.Items.Add(discardMenuItem);

            commitMenuItem = new ToolStripMenuItem("Commit on Reboot");
            commitMenuItem.Click += new EventHandler(notify_setCommit);
            contextmenu.Items.Add(commitMenuItem);

            disableMenuItem = new ToolStripMenuItem("Commit && Disable on Reboot");
            disableMenuItem.Click += new EventHandler(notify_setDisable);
            contextmenu.Items.Add(disableMenuItem);

            ToolStripSeparator sep = new ToolStripSeparator();
            contextmenu.Items.Add(sep);

            ToolStripMenuItem item = new ToolStripMenuItem("Commit && Disable NOW");
            item.Click += new EventHandler(notify_setDisableNow);
            contextmenu.Items.Add(item);

            sep = new ToolStripSeparator();
            contextmenu.Items.Add(sep);

            item = new ToolStripMenuItem("Config");
            item.Click += new EventHandler(notify_config);
            contextmenu.Items.Add(item);
            
            sep = new ToolStripSeparator();
            contextmenu.Items.Add(sep);

            item = new ToolStripMenuItem("Exit");
            item.Click += new EventHandler(Menu_OnExit);
            contextmenu.Items.Add(item);
            

            //notifyicon
            notify = new NotifyIcon();
            notify.Icon = iconoff;
            notify.Text = "";
            notify.BalloonTipTitle = "EwfUtil Warning";
            notify.BalloonTipText = "Your EWF overlay is getting full. Commit and/or reboot now to secure your data.";
            notify.ContextMenuStrip = contextmenu;
            //notify.DoubleClick += new EventHandler(notify_DoubleClick); //show form when double clicked
            notify.Visible = true;

            //timer for sample actions
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = this.timeout * 1000;
            timer.Start();

            if (startup == "Commit")
                UpdateStatus("-commit");
            else if (startup == "Discard")
                UpdateStatus("-nocmd");
            else
                UpdateStatus("");
            
        }

        //you could do some real checking here
        void timer_Tick(object sender, EventArgs e)
        {
            UpdateStatus("");
        }

        public void restartTimer()
        {
            timer.Stop();
            timer.Interval = this.timeout * 1000;
            timer.Start();
        }

        private void UpdateStatus(String args)
        {
            enabled = false;

            ProcessStartInfo psi = new ProcessStartInfo("ewfmgr.exe");
            psi.Arguments = String.Format("{0} {1}", args, drive);
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            Process p = Process.Start(psi);

            String line;

            usage = 0;

            char[] chars = new char[] { 'b', 'y', 't', 'e', 's', ' ' };

            while ((line = p.StandardOutput.ReadLine()) != null)
            {
                if (line.Trim().ToLower().StartsWith("boot command"))
                    boot_cmd = line.Substring(line.IndexOf("ommand") + 7).Trim();

                if (line.Trim().ToLower().StartsWith("state"))
                    enabled = line.Substring(line.IndexOf("state") + 8).Trim().ToLower().Equals("enabled");
                    //System.Windows.Forms.MessageBox.Show(line.Substring(line.IndexOf("state") + 6));

                if (line.Trim().ToLower().StartsWith("memory used for data"))
                    usage += Int32.Parse(line.Substring(line.IndexOf("data") + 5).Trim(chars));

                if (line.Trim().ToLower().StartsWith("memory used for mapping"))
                    usage += Int32.Parse(line.Substring(line.IndexOf("ping") + 5).Trim(chars));

            }

            discardMenuItem.Checked = false;
            commitMenuItem.Checked = false;
            disableMenuItem.Checked = false;
            enableMenuItem.Checked = false;

            if (boot_cmd == "NO_CMD")
            {
                discardMenuItem.Checked = true;
            }
            else if (boot_cmd == "COMMIT")
            {
                commitMenuItem.Checked = true;
            }
            else if (boot_cmd == "DISABLE")
            {
                disableMenuItem.Checked = true;
            }
            else if (boot_cmd == "ENABLE")
            {
                enableMenuItem.Checked = true;
            }


            if (enabled)
            {

                float u = usage / (1024 * 1024);
                notify.Text = String.Format("EWF Usage: {0:##0.0} MB", u);


                if (u > threshold)
                {
                    notify.Icon = iconwarn;
                    notify.ShowBalloonTip(5000);
                }
                else
                    notify.Icon = iconok;
            }
            else
            {
                notify.Icon = iconoff;
                notify.Text = "EWF disabled";
            }
        }

        void notify_config(object sender, EventArgs e)
        {
            //show the form for settings
            //prevent user from creating more then one form
            if (!formPresent)
            {
                formPresent = true;
                form = new Config();
                //use close event to reset formpresent boolean
                form.FormClosed += new FormClosedEventHandler(form_FormClosed);
                form.Show();
            }
        }

        void form_FormClosed(object sender, FormClosedEventArgs e)
        {
            formPresent = false;
            form = null;
        }


        ~Poller()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                timer.Stop();
                IsDisposing = true;
            }
        }

        void Menu_OnExit(Object sender, EventArgs e)
        {
            //be sure to call Application.Exit
            notify.Visible = false;
            Dispose();
            Application.Exit();
        }

        void notify_setDiscard(Object sender, EventArgs e)
        {
            UpdateStatus("-nocmd");
        }

        void notify_setCommit(Object sender, EventArgs e)
        {
            UpdateStatus("-commit");
        }

        void notify_setDisable(Object sender, EventArgs e)
        {
            UpdateStatus("-commitanddisable");
        }

        void notify_setDisableNow(Object sender, EventArgs e)
        {
            UpdateStatus("-commitanddisable -live");
        }

        void notify_setEnable(Object sender, EventArgs e)
        {
            UpdateStatus("-enable");
        }
    }
}
