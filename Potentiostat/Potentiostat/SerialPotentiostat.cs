using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace Potentiostat
{
    class SerialPotentiostat
    {
        private int _Buffer = 0;
        private System.Threading.CancellationTokenSource CancelListen;
        private ulong _TotalUpdates = 0;
        private bool reallyconnected;
        private bool simconnected;
        private SerialPort Port;
        private string Command;
        public delegate void NewDataEventHandler(object sender, NewDataEventArgs e);
        public event NewDataEventHandler NewData;
        public event EventHandler Connected;
        public delegate void DisconnectedEventHandler(object sender, DisconnectedEventArgs e);
        public event DisconnectedEventHandler Disconnected;
        public delegate void GotSingleShotEventHander(object sender, SingleShotEventArgs e);
        public event GotSingleShotEventHander GotSingleShot;
        public int Buffer { get { return _Buffer; } }
        public ulong TotalUpdates { get { return _TotalUpdates; } }
        private SynchronizationContext context;

        public SerialPotentiostat()
        {
            _Buffer = 0;
            _TotalUpdates = 0;
            Command = null;
            reallyconnected = false;
            simconnected = false;
            context = SynchronizationContext.Current;
        }


        public void ExecuteCommand(string Command)
        {
            this.Command = Command;
        }

        public bool IsConnected
        {
            get
            {
                return simconnected || (Port != null && Port.IsOpen && reallyconnected);
            }
        }
     

        public string Connect(string COMPort, int Baudrate)
        {
            if (IsConnected) return "Already open";
            Port = new SerialPort(COMPort, Baudrate);
            try
            {
                Port.Open();
            }
            catch (Exception ex)
            {
                return "Error opening serial port: " + ex.Message;
            }
            CancelListen = new System.Threading.CancellationTokenSource();
            Task.Run(new Action(() => ReceivingLoop()));
            context.Post(new SendOrPostCallback(delegate (object state)
            {
                Connected?.Invoke(this, new EventArgs());
            }), null);
            return "OK";
        }
        public void Disconnect()
        {
            if (!IsConnected) return;
            CancelListen.Cancel();
        }
        public string SimConnect(int delay)
        {
            if (IsConnected) return "Already open";
            CancelListen = new System.Threading.CancellationTokenSource();
            Task.Run(() => SimReceivingLoop(delay));
            context.Post(new SendOrPostCallback(delegate (object state)
            {
                Connected?.Invoke(this, new EventArgs());
            }), null);
            return "OK";
        }
        private void ReceivingLoop()
        {
            var rx = new Regex("m(\\d+)u(\\d+)E(\\d+)I(\\d+)");
            Port.DiscardInBuffer();
            int fails = 0;
            Port.ReadTimeout = 10000;
            reallyconnected = true;
            _TotalUpdates = 0;
            try
            {
                do
                {
                    //Write commands
                    if (Command != null)
                    {
                        Port.WriteLine(Command);
                        Command = null;
                    }
                    //Read some data when available, or wait
                    if (Port.BytesToRead > 0)
                    {
                        var line = Port.ReadLine();
                        if(line.StartsWith("SSSTART"))
                        {
                            //Record Single Shot
                            List<ulong> Time = new List<ulong>();
                            List<int> E = new List<int>();
                            List<int> I = new List<int>();
                            do
                            {
                                line = Port.ReadLine();
                                if (rx.IsMatch(line))
                                {
                                    var SSmatch = rx.Match(line);
                                    var SSthisMilli = ulong.Parse(SSmatch.Groups[1].Value);
                                    var SSthisMicro = ulong.Parse(SSmatch.Groups[2].Value);
                                    var SSthisE = int.Parse(SSmatch.Groups[3].Value);
                                    var SSthisI = int.Parse(SSmatch.Groups[4].Value);
                                    Time.Add(SSthisMicro);
                                    E.Add(SSthisE);
                                    I.Add(SSthisI);
                                }
                            } while (line != "SSEND");
                            context.Post(new SendOrPostCallback(delegate (object state)
                            {
                                GotSingleShot?.Invoke(this, new SingleShotEventArgs(Time, E, I));
                            }), null);
                            continue;
                        }
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
                        NewData?.Invoke(this, new NewDataEventArgs(thisMilli, thisMicro, thisE, thisI));
                        _TotalUpdates++;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(5);
                    }
                    _Buffer = Port.BytesToRead;
                } while (!CancelListen.IsCancellationRequested);
                //Disconnected?.Invoke(this, new DisconnectedEventArgs("Cancel"));
                context.Post(new SendOrPostCallback(delegate (object state)
                {
                    Disconnected?.Invoke(this, new DisconnectedEventArgs("Cancel"));
                }), null);
            }
            catch (Exception ex)
            {
                //Disconnected?.Invoke(this, new DisconnectedEventArgs("Exception: " + ex.Message));
                context.Post(new SendOrPostCallback(delegate (object state)
                {
                    Disconnected?.Invoke(this, new DisconnectedEventArgs("Exception: " + ex.Message));
                }), null);
            } finally
            {
                Port?.Close();
                reallyconnected = false;
            }
        }
        private void SimReceivingLoop(int delay)
        {
            DateTime starttime = DateTime.Now;
            simconnected = true;
            do
            {
                var thisE = (int)(1023 * Math.Sin(DateTime.Now.Millisecond / 1000.0 * 2 * Math.PI));
                var thisI = (int)(1023 * Math.Cos(DateTime.Now.Millisecond / 1000.0 * 2 * Math.PI));
                var milli = (ulong)((DateTime.Now - starttime).TotalMilliseconds);
                NewData?.Invoke(this, new NewDataEventArgs(milli, 0, thisE, thisI));
                _Buffer = 123;
                System.Threading.Thread.Sleep(delay);
            } while (!CancelListen.IsCancellationRequested);
            simconnected = false;
            //Disconnected?.Invoke(this, new DisconnectedEventArgs("Cancel"));
            context.Post(new SendOrPostCallback(delegate (object state)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs("Cancel"));
            }), null);
        }

    }
}
