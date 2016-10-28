using System;
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
            Settings.CurrentCalibm = Potentiostat.Properties.Settings.Default.CurrentCalibm;
            Settings.CurrentCalibb = Potentiostat.Properties.Settings.Default.CurrentCalibb;
            Settings.VoltageThresholdP = Potentiostat.Properties.Settings.Default.EThreshP;
            Settings.VoltageThresholdN = Potentiostat.Properties.Settings.Default.EThreshN;
            Settings.CurrentThresholdPPerc = Potentiostat.Properties.Settings.Default.IThreshP;
            Settings.CurrentThresholdNPerc = Potentiostat.Properties.Settings.Default.IThreshN;
            var ShuntsToActive = Potentiostat.Properties.Settings.Default.ActiveShunts.Split('|');
            foreach (var s in Settings.Shunts) s.Active = ShuntsToActive.Contains(s.Name);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());

            Potentiostat.Properties.Settings.Default.AverageWindow = Settings.Averaging;
            Potentiostat.Properties.Settings.Default.VoltageCalim= Settings.VoltageCalibm;
            Potentiostat.Properties.Settings.Default.VoltageCalib=Settings.VoltageCalibb;
            Potentiostat.Properties.Settings.Default.CurrentCalibm = Settings.CurrentCalibm;
            Potentiostat.Properties.Settings.Default.CurrentCalibb = Settings.CurrentCalibb;
            Potentiostat.Properties.Settings.Default.EThreshP= Settings.VoltageThresholdP;
            Potentiostat.Properties.Settings.Default.EThreshN=Settings.VoltageThresholdN;
            Potentiostat.Properties.Settings.Default.IThreshP=Settings.CurrentThresholdPPerc;
            Potentiostat.Properties.Settings.Default.IThreshN=Settings.CurrentThresholdNPerc;
            var activeshunts = Settings.Shunts.Where(s => s.Active).Select(s => s.Name);
            Potentiostat.Properties.Settings.Default.ActiveShunts = string.Join("|", activeshunts);
            Potentiostat.Properties.Settings.Default.Save();
        }
    }
}
