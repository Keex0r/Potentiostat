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
using System.Text.RegularExpressions;

namespace Potentiostat
{
    public partial class frmMain : Form
    {

        public BindingSource bdsShunts;
        private DateTime LastUpdate = DateTime.Now;
        private int LastWE=0;
        private int LastCurrent=0;
        private SerialPort Port;
        private int Buffer = 0;
        private event EventHandler ValuesChanged;
        private System.Threading.CancellationTokenSource CancelListen;
        private int TotalUpdates = 0;
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
            ValuesChanged += FrmMain_ValuesChanged;
        }

        private void FrmMain_ValuesChanged(object sender, EventArgs e)
        {
            UpdateValueDisplay();
        }
        private void UpdateValueDisplay()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateValueDisplay()));
                return;
            }
            lblVoltage.Text = NumberToString(Program.Settings.GetVoltage(LastWE), "V");
            lblCurrent.Text = NumberToString(Program.Settings.GetCurrent(LastCurrent), "A");
            tslBuffer.Text = Buffer.ToString();
            tslUpdates.Text = TotalUpdates.ToString();
            chart1.Series["Volt"].Points.AddXY(LastUpdate, Program.Settings.GetVoltage(LastWE));
            chart1.Series["Current"].Points.AddXY(LastUpdate, Program.Settings.GetCurrent(LastCurrent));
            if (chart1.Series["Volt"].Points.Count() > 5000) chart1.Series["Volt"].Points.RemoveAt(0);
            if (chart1.Series["Current"].Points.Count() > 5000) chart1.Series["Current"].Points.RemoveAt(0);
            chart1.ChartAreas[0].AxisX.Minimum = chart1.Series["Volt"].Points.First().XValue;
            chart1.ChartAreas[0].AxisX.Maximum = chart1.Series["Volt"].Points.Last().XValue ;
        }
        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private async void Connect(string com, int Baud)
        {
            if (Port != null && Port.IsOpen) return;
            Port = new SerialPort(com, Baud);
            try
            {
                Port.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening serial port: " + ex.Message);
                return;
            }
            btnConnect.Enabled = false;
            tbCOM.Enabled = false;
            numBaud.Enabled = false;
            btnDisconnect.Enabled = true;
            CancelListen = new System.Threading.CancellationTokenSource();
            TotalUpdates = 0;
            int queuelength = 15;
            await Task.Run(new Action(() =>
            {
                var rx = new Regex("E(\\d+)I(\\d+)");
                var EQueue = new Queue<int>();
                var IQueue = new Queue<int>();
                do
                {
                    var line = Port.ReadLine();
                    if (!rx.IsMatch(line)) continue;
                    var match = rx.Match(line);
                    var thisE = int.Parse(match.Groups[1].Value);
                    var thisI = int.Parse(match.Groups[2].Value);
                    EQueue.Enqueue(thisE);
                    IQueue.Enqueue(thisI);
                    if (EQueue.Count() > queuelength) EQueue.Dequeue();
                    if (IQueue.Count() > queuelength) IQueue.Dequeue();
                    LastWE = (int)EQueue.Average();
                    LastCurrent = (int)IQueue.Average();
                    LastUpdate = DateTime.Now;
                    Buffer = Port.BytesToRead;
                    TotalUpdates++;
                    if (ValuesChanged != null) ValuesChanged(this, new EventArgs());
                    Port.BaseStream.Flush();
                   Port.DiscardInBuffer();
                } while (!CancelListen.IsCancellationRequested);
            }));
            Port.Close();
            btnConnect.Enabled = true;
            tbCOM.Enabled = true;
            numBaud.Enabled = true;
            btnDisconnect.Enabled = false;
        }

        private void BdsShunts_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged) UpdateCurrentRangeDisplay();
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
        private void UpdateBufferDisplay(int Buffer)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateBufferDisplay(Buffer)));
                return;
            }
            tslBuffer.Text = Buffer.ToString();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Connect(tbCOM.Text,(int)numBaud.Value);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (CancelListen != null) CancelListen.Cancel();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CancelListen != null  && CancelListen.IsCancellationRequested == false)
            {
                MessageBox.Show("Please disconnect first.");
                e.Cancel = true;
            }
        }
    }
}
