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
using System.Diagnostics;

namespace Potentiostat
{
    public partial class frmMain : Form
    {
        private SerialPotentiostat Device;

        public BindingSource bdsShunts;

        private string LogPath;
        private double LastLogEntry;
        private bool _DoLog;
        private bool DoLog
        {
            get
            {
                return _DoLog;
            }
            set
            {
                _DoLog = value;
                if(value) SetLogpathDisplay(LogPath); else SetLogpathDisplay(null);
            }
        }

        private Queue<Misc.RawDataPoint> Data;
        private Queue<Tuple<double,double,double>> AverageBuffer;

        public frmMain()
        {
            InitializeComponent();
            tbCOM.Text = Potentiostat.Properties.Settings.Default.COM;
            numBaud.Value = Potentiostat.Properties.Settings.Default.Baud;
            tbLogPath.Text = Potentiostat.Properties.Settings.Default.LogPath;

            chart1.Series.LinePenDefaultWidthY1 = 1;
            chart1.Series.LinePenDefaultWidthY2 = 1;
            var se = chart1.Series.AddSeries(jwGraph.jwGraph.Series.enumSeriesType.Line, jwGraph.jwGraph.Axis.enumAxisLocation.Primary);
            se.Name = "Volt";
            se.LegendText = "Voltage / V";
            var si = chart1.Series.AddSeries(jwGraph.jwGraph.Series.enumSeriesType.Line, jwGraph.jwGraph.Axis.enumAxisLocation.Secondary);
            si.Name = "Current";
            si.LegendText = "Current / A";
            chart1.XAxis.Title = "Time / s";
            chart1.Y1Axis.Title = "Voltage / V";
            chart1.Y2Axis.Title = "Current / A";
            chart1.HighQuality = false;

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
            Data = new Queue<Misc.RawDataPoint>();
            AverageBuffer = new  Queue<Tuple<double,double, double>>();
            DoLog = false;
            propertyGrid1.SelectedObject = Program.Settings;
            Device = new SerialPotentiostat();
            Device.Connected += HandleConnected;
            Device.Disconnected += HandleDisconnect;
            Device.NewData += HandleNewValues;
            Device.GotSingleShot += Device_GotSingleShot;
        }

        private void Device_GotSingleShot(object sender, SingleShotEventArgs e)
        {
            var frm = new frmSingleShot();
            frm.Show(e);
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void HandleConnected(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            tbCOM.Enabled = false;
            numBaud.Enabled = false;
            btnDisconnect.Enabled = true;
            chart1.Series["Volt"].ClearData();
            chart1.Series["Current"].ClearData();
            Data.Clear();
            AverageBuffer.Clear();
            tmrUpdate.Start();

        }
        private void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            tmrUpdate.Stop();
            btnConnect.Enabled = true;
            tbCOM.Enabled = true;
            numBaud.Enabled = true;
            btnDisconnect.Enabled = false;
        }

        private void Connect(string com, int Baud)
        {
           Device.Connect(com, Baud);
        }
        private void SimConnect(int Wait)
        {
            Device.SimConnect(Wait);
        }


        private void HandleNewValues(object sender, NewDataEventArgs e)
        {
            var d = new Misc.RawDataPoint(e.millis, e.micros, e.Potential, e.Current);
            lock (Data)
            {
                Data.Enqueue(d);
            }
        }

        private void BdsShunts_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged) UpdateCurrentRangeDisplay();
        }

        private void SetELimit(bool state)
        {
            if (state == tslELimit.Visible) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetELimit(state)));
                return;
            }
            tslELimit.Visible = state;
        }
        private void SetILimit(bool state)
        {
            if (state == tslILimit.Visible) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetILimit(state)));
                return;
            }
            tslILimit.Visible = state;
        }

        private void UpdateCurrentRangeDisplay()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateCurrentRangeDisplay()));
                return;
            }
            var CurrentRange = Program.Settings.GetCurrentRange();
            var res = Program.Settings.GetCurrentResolution();
            tslCurrentRange.Text = Misc.NumberToString(CurrentRange.Item1, "A") + " -> " + Misc.NumberToString(CurrentRange.Item2, "A") + " @ " + Misc.NumberToString(res, "A");
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
            if (!cbSim.Checked)
            {
                Connect(tbCOM.Text, (int)numBaud.Value);
            }
            else
            {
                SimConnect(10);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Device.Disconnect();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Device.IsConnected)
            {
                MessageBox.Show("Please disconnect first.");
                e.Cancel = true;
            }
            Potentiostat.Properties.Settings.Default.Baud = (int)numBaud.Value;
            Potentiostat.Properties.Settings.Default.COM = tbCOM.Text;
            Potentiostat.Properties.Settings.Default.LogPath = tbLogPath.Text;
            Potentiostat.Properties.Settings.Default.Save();
        }

        private void tmrUpdate_Tick(object sender, EventArgs e)
        {
            tmrUpdate.Stop();
            Tuple<double,double> d = null;
            double avgDate = 0.0;
            double dE = 0.0;
            lock (Data)
            {
                if (DoLog)
                {
                    try
                    {
                        if (!File.Exists(LogPath)) File.AppendAllLines(LogPath, new string[] { "Timestap / s\tVoltage / V\tCurrent / A" });
                    }
                    catch (Exception)
                    {
                    }
                }

                //Add all data to the chart
                chart1.BeginUpdate();
                var sb = new System.Text.StringBuilder();
                while (Data.Count > 0)
                {
                    var td = Data.Dequeue();
                    var thisE = Program.Settings.GetVoltage(td.E);
                    var thisI = Program.Settings.GetCurrent(td.I);
                    //Check thresholds
                    var DoContinue = false;
                    if (thisE > Program.Settings.VoltageThresholdP || thisE < Program.Settings.VoltageThresholdN)
                    {
                        SetELimit(true);
                        DoContinue = true;
                    }
                    else SetELimit(false);

                    var range = Program.Settings.GetCurrentRange();
                    if (thisI < Program.Settings.CurrentThresholdNPerc / 100.0 * range.Item1 || thisI > Program.Settings.CurrentThresholdPPerc / 100.0 * range.Item2)
                    {
                        SetILimit(true);
                        DoContinue = true;
                    }
                    else SetILimit(false);
                    if (DoContinue) continue;
                    AverageBuffer.Enqueue(Tuple.Create(td.Time,thisE,thisI));
                    if (DoLog && (td.Time-LastLogEntry) * 1000 > Program.Settings.LogDelay)
                    {
                        var logE = thisE;
                        var logI = thisI;
                        sb.AppendLine(td.Time.ToString() + "\t" + logE.ToString() + "\t" + logI.ToString());
                        LastLogEntry = td.Time;
                    }
                    while(AverageBuffer.Count() > Program.Settings.Averaging) AverageBuffer.Dequeue();
                    
                    var avgE = AverageBuffer.Select(value => value.Item2).Average();
                    var avgI = AverageBuffer.Select(value => value.Item3).Average();
                    avgDate = AverageBuffer.Select(value => value.Item1).Average();
                    dE = GetdEdt(AverageBuffer);
                    d = Tuple.Create(avgE, avgI);
                    var realE = avgE;
                    var realI = avgI;

                    if (chart1.Series["Volt"].Datapoints.Count() < 1 || avgDate - chart1.Series["Volt"].Datapoints.Last().X > 10 / 1000.0)
                    {
                        chart1.Series["Volt"].AddXY(avgDate, realE);
                        chart1.Series["Current"].AddXY(avgDate, realI);
                        if (chart1.Series["Volt"].Datapoints.Count() > 2500) chart1.Series["Volt"].RemoveAt(0);
                        if (chart1.Series["Current"].Datapoints.Count() > 2500) chart1.Series["Current"].RemoveAt(0);
                    }

                }
                if (DoLog)
                {
                    try
                    {
                        File.AppendAllText(LogPath, sb.ToString());
                    }
                    catch (Exception)
                    {
                    }
                }
                chart1.EndUpdate();
            }
            if (d != null)
            {
                lblVoltage.Text = Misc.NumberToString(d.Item1, "V");
                lblCurrent.Text = Misc.NumberToString(d.Item2, "A");
                lbldEdt.Text = Misc.NumberToString(dE, "V/s");
                tslBuffer.Text = Device.Buffer.ToString();
                tslUpdates.Text = Device.TotalUpdates.ToString();
            }
            tmrUpdate.Start();
        }

        private double GetdEdt(IEnumerable<Tuple<double,double,double>> Data)
        {
            var datalist = Data.ToList();
            var elist = new List<double>();
            var tlist = new List<double>();
            for (int i = 0; i < Data.Count(); i++)
            {
                var E = datalist[i].Item2;
                elist.Add(E);
                var x1 = datalist[i].Item1;
                tlist.Add(x1);
            }
            var avgE = elist.Average();
            var avgt=tlist.Average();

            double sum1 = 0.0;
            double sum2 = 0.0;
            
            for (int i = 0; i < Data.Count(); i++)
            {
                sum1 += (tlist[i] - avgt) * (elist[i] - avgE);
                sum2 += (tlist[i] - avgt) * (tlist[i] - avgt);
            }
            return sum1/sum2;
        }

        private void btnBrowseLog_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog sfd = new FolderBrowserDialog())
            {
                if (sfd.ShowDialog(this) == DialogResult.Cancel) return;
                tbLogPath.Text = sfd.SelectedPath;
            }
        }

        private void btnStartLog_Click(object sender, EventArgs e)
        {
            if(!Directory.Exists(tbLogPath.Text))
            {
                MessageBox.Show("Path does not exist!");
                return;
            }
            tbLogPath.Enabled = false;
            btnBrowseLog.Enabled = false;
            btnStartLog.Enabled = false;
            btnStopLog.Enabled = true;
            btnClearLog.Enabled = true;
            LogPath = GetLogfilename(tbLogPath.Text);
            DoLog = true;
        }
        private string GetLogfilename(string path)
        {
            int counter = 0;
            while (File.Exists(Path.Combine(path, "PLog_" + counter.ToString("0000") + ".txt")))
            {
                counter++;
            }
            return Path.Combine(path, "PLog_" + counter.ToString("0000") + ".txt");
        }
        private void btnStopLog_Click(object sender, EventArgs e)
        {
            DoLog = false;

            tbLogPath.Enabled = true;
            btnBrowseLog.Enabled = true;
            btnStartLog.Enabled = true;
            btnStopLog.Enabled = false;
            btnClearLog.Enabled = false;
            LogPath = tbLogPath.Text;
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            if (!DoLog) return;
            DoLog = false;
            LogPath = GetLogfilename(tbLogPath.Text);
            DoLog = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chart1.Series["Volt"].ClearData();
            chart1.Series["Current"].ClearData();
        }

        private void SetLogpathDisplay(string path)
        {
            if (path == null) tslLogWhere.Text = "Not logging."; else tslLogWhere.Text = path;
        }

        private void btnSetHigh_Click(object sender, EventArgs e)
        {
            Device.ExecuteCommand("CEHIGH");
        }

        private void btnSetLow_Click(object sender, EventArgs e)
        {
            Device.ExecuteCommand("CELOW");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Device.ExecuteCommand("STARTSS");
        }
    }
}
