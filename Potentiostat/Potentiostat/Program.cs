﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Potentiostat
{
    static class Program
    {

        public static Settings Settings;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Settings = new Settings();
            Settings.Averaging = Potentiostat.Properties.Settings.Default.AverageWindow;
            Settings.VoltageCalibm = Potentiostat.Properties.Settings.Default.VoltageCalim;
            Settings.VoltageCalibb = Potentiostat.Properties.Settings.Default.VoltageCalib;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());

            Potentiostat.Properties.Settings.Default.AverageWindow = Settings.Averaging;
            Potentiostat.Properties.Settings.Default.VoltageCalim= Settings.VoltageCalibm;
            Potentiostat.Properties.Settings.Default.VoltageCalib=Settings.VoltageCalibb;
            Potentiostat.Properties.Settings.Default.Save();
        }
    }
}
