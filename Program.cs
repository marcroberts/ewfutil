using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace EwfUtil
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /******************** NO Form ********************************************************
            /* Here you create the class that will serve as the main
             * instead of loading the form and then hiding it. 
             * The class should call Application.Exit() when exit time comes.
             * The form will only be created and kept in memory when its actually used.
             * Note that Application.Run does not start any form
             * Application.SetCompatibleTextRenderingDefault() should not be called when there is no form
             * Checker is a Singleton
             *************************************************************************************/
            Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            Poller checker = Poller.GetCheckerObject();
            Application.Run();
            checker = null; //must be done to stop referencing Checker so GC can collect it.
        }
    }
}