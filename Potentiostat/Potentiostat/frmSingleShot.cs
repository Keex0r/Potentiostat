using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Potentiostat
{
    public partial class frmSingleShot : Form
    {
        public frmSingleShot()
        {
            InitializeComponent();
            var se = graph.Series.AddSeries(jwGraph.jwGraph.Series.enumSeriesType.Line, jwGraph.jwGraph.Axis.enumAxisLocation.Primary);
            se.Name = "Voltage";
            se.LegendText = "Voltage / V";
            var si = graph.Series.AddSeries(jwGraph.jwGraph.Series.enumSeriesType.Line, jwGraph.jwGraph.Axis.enumAxisLocation.Secondary);
            si.Name = "Current";
            si.LegendText = "Current / A";
            graph.XAxis.Title = "Time / s";
            graph.Y1Axis.Title = "Voltage / V";
            graph.Y2Axis.Title = "Current / A";
            graph.Message = DateTime.Now.ToString();
        }
        public void Show(SingleShotEventArgs e)
        {
            graph.BeginUpdate();
            foreach(var r in e.Data)
            {
                var E = Program.Settings.GetVoltage(r.E);
                var I = Program.Settings.GetCurrent(r.I);
                graph.Series["Voltage"].AddXY(r.Time, E);
                graph.Series["Current"].AddXY(r.Time, I);
            }
            graph.EndUpdate();
            this.Show();
        }
        
    }
}
