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

        public BindingSource bdsShunts;
        private SerialPort Port;
        private int Buffer = 0;
        private System.Threading.CancellationTokenSource CancelListen;
        private int TotalUpdates = 0;
        private string LogPath;
        private string Command;
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

            chart1.Series.LinePenDefaultWidthY1 = 3;
            chart1.Series.LinePenDefaultWidthY2 = 3;
            chart1.Series.AddSeries(jwGraph.jwGraph.Series.enumSeriesType.Line, jwGraph.jwGraph.Axis.enumAxisLocation.Primary).Name = "Volt";
            chart1.Series.AddSeries(jwGraph.jwGraph.Series.enumSeriesType.Line, jwGraph.jwGraph.Axis.enumAxisLocation.Secondary).Name = "Current";

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
            Command = null;
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
            chart1.Series["Volt"].ClearData();
            chart1.Series["Current"].ClearData();
            Data.Clear();
            AverageBuffer.Clear();
            tmrUpdate.Start();
            try
            {
                await Task.Run(new Action(() =>
                {
                    var rx = new Regex("m(\\d+)u(\\d+)E(\\d+)I(\\d+)");
                    Port.DiscardInBuffer();
                    int fails = 0;
                    Port.ReadTimeout = 10000;
                    do
                    {
                        if (Command != null)
                        {
                            Port.WriteLine(Command);
                            Command = null;
                        }
                        var line = Port.ReadLine();
                        if (!rx.IsMatch(line))
                        {
                            fails++;
                            if (fails > 10) throw new Exception("No matching text. Baudrate wrong?");
                            continue;
                        }
                        fails = 0;
                        var match = rx.Match(line);
                        var thisMilli = ulong.Parse(match.Groups[1].Value);
                        var thisMicro = ulong.Parse(match.Groups[2].Value);
                        var thisE = int.Parse(match.Groups[3].Value);
                        var thisI = int.Parse(match.Groups[4].Value);
                        HandleNewValues(thisMilli, thisMicro, thisE, thisI);
                        Buffer = Port.BytesToRead;
                    } while (!CancelListen.IsCancellationRequested);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loop error: " + ex.Message);
            }
            try
            {
                Port.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error closing serial port: " + ex.Message);
            }
            tmrUpdate.Stop();

            btnConnect.Enabled = true;
            tbCOM.Enabled = true;
            numBaud.Enabled = true;
            btnDisconnect.Enabled = false;
        }
        private async void SimConnect(int Wait)
        {
            btnConnect.Enabled = false;
            tbCOM.Enabled = false;
            numBaud.Enabled = false;
            btnDisconnect.Enabled = true;
            CancelListen = new System.Threading.CancellationTokenSource();
            TotalUpdates = 0;
            tmrUpdate.Start();
            chart1.Series["Volt"].ClearData();
            chart1.Series["Current"].ClearData();
            var starttime = DateTime.Now;
            await Task.Run(new Action(() =>
            {
                do
                {
                    var thisE = (int)(1023 * Math.Sin(DateTime.Now.Millisecond / 1000.0 * 2 * Math.PI));
                    var thisI = (int)(1023 * Math.Cos(DateTime.Now.Millisecond / 1000.0 * 2 * Math.PI));
                    var milli = (ulong)((DateTime.Now - starttime).TotalMilliseconds);
                    HandleNewValues(milli, 0, thisE, thisI);
                    Buffer = 123;
                    System.Threading.Thread.Sleep(Wait);
                } while (!CancelListen.IsCancellationRequested);
            }));
            tmrUpdate.Stop();
            btnConnect.Enabled = true;
            tbCOM.Enabled = true;
            numBaud.Enabled = true;
            btnDisconnect.Enabled = false;
        }


        private void HandleNewValues(ulong Milli, ulong Micro, int E, int I)
        {
            var d = new Misc.RawDataPoint(Milli, Micro, E, I);
            lock (Data)
            {
                Data.Enqueue(d);
            }
            TotalUpdates++;
        }

        private void BdsShunts_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged) UpdateCurrentRangeDisplay();
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
            if (CancelListen != null) CancelListen.Cancel();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CancelListen != null && CancelListen.IsCancellationRequested == false)
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
                    AverageBuffer.Enqueue(Tuple.Create(td.Time,thisE,thisI));
                    if (DoLog)
                    {
                        var logE = thisE;
                        var logI = thisI;
                        sb.AppendLine(td.Time.ToString() + "\t" + logE.ToString() + "\t" + logI.ToString());
                    }
                    while(AverageBuffer.Count() > Program.Settings.Averaging) AverageBuffer.Dequeue();
                    
                    var avgE = AverageBuffer.Select(value => value.Item2).Average();
                    var avgI = AverageBuffer.Select(value => value.Item3).Average();
                    avgDate = AverageBuffer.Select(value => value.Item1).Average();
                    dE = GetdEdt(AverageBuffer);
                    d = Tuple.Create(avgE, avgI);
                    var realE = avgE;
                    var realI = avgI;

                    if (chart1.Series["Volt"].Datapoints.Count() < 1 || avgDate - chart1.Series["Volt"].Datapoints.Last().X > 5 / 1000.0)
                    {
                        chart1.Series["Volt"].AddXY(avgDate, realE);
                        chart1.Series["Current"].AddXY(avgDate, realI);
                        if (chart1.Series["Volt"].Datapoints.Count() > 5000) chart1.Series["Volt"].RemoveAt(0);
                        if (chart1.Series["Current"].Datapoints.Count() > 5000) chart1.Series["Current"].RemoveAt(0);
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
                tslBuffer.Text = Buffer.ToString();
                tslUpdates.Text = TotalUpdates.ToString();
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
            Command = "CEHIGH";
        }

        private void btnSetLow_Click(object sender, EventArgs e)
        {
            Command = "CELOW";
        }
    }
}
