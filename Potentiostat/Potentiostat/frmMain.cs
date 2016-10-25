using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace Potentiostat
{
    public partial class frmMain : Form
    {

        public BindingSource bdsShunts;
        private DateTime LastUpdate;
        private int LastWE;
        private int LastCurrent;
        private SerialPort Port;

        public frmMain()
        {
            InitializeComponent();
            bdsShunts = new BindingSource();
            bdsShunts.DataSource = Program.Settings.Shunts;
            dataGridView1.AutoGenerateColumns = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.DataSource = bdsShunts;
            bdsShunts.ListChanged += BdsShunts_ListChanged;
            UpdateCurrentRangeDisplay();
            dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void BdsShunts_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged) UpdateCurrentRangeDisplay();
        }

        public double GetCurrentVoltage
        {
            get
            {
                double Eana = LastWE / 1023 * Program.Settings.Vp;
                double EReal = Program.Settings.WEVoltSenseDivider.GetVIn(Eana);
                return EReal;
            }
        }
        public double GetCurrentCurrent
        {
            get
            {
                double Eana = LastCurrent / 1023 * Program.Settings.Vp;
                double EReal = Program.Settings.CurrentSenseDivider.GetVIn(Eana);
                return ((Shunt)bdsShunts.Current).GetCurrent(EReal);
            }
        }

        private string NumberToString(double v,string Unit)
        {
            if(Math.Abs(v)<1e-9)
            {
                return (v * 1e12).ToString("0.000") + " f" + Unit;
            }
            else if (Math.Abs(v) < 1e-6)
            {
                return (v * 1e9).ToString("0.000") + " n" + Unit;
            }
            else if (Math.Abs(v) < 1e-3)
            {
                return (v * 1e6).ToString("0.000") + " µ" + Unit;
            }
            else if (Math.Abs(v) < 1)
            {
                return (v * 1e3).ToString("0.000") + " m" + Unit;
            }
            else
            {
                return v.ToString("0.000");
            }
        }

        private void UpdateCurrentRangeDisplay()
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateCurrentRangeDisplay()));
                return;
            }
            var CurrentRange = Program.Settings.GetCurrentRange();
            var res = Program.Settings.GetCurrentResolution();
            tslCurrentRange.Text = NumberToString(CurrentRange.Item1, "A") + " -> " + NumberToString(CurrentRange.Item2, "A") + " @ " + NumberToString(res, "A");
        }


       
    }
}
