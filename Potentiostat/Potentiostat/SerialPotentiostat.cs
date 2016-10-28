using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Potentiostat
{
    class SerialPotentiostat
    {
        private int _Buffer = 0;
        private System.Threading.CancellationTokenSource CancelListen;
        private int _TotalUpdates = 0;
        private bool reallyconnected;
        private bool simconnected;
        private SerialPort Port;
        private string Command;
        public delegate void NewDataEventHandler(object sender, NewDataEventArgs e);
        public event NewDataEventHandler NewData;
        public event EventHandler Connected;
        public delegate void DisconnectedEventHandler(object sender, DisconnectedEventArgs e);
        public event DisconnectedEventHandler Disconnected;
        public int Buffer { get { return _Buffer; } }
        public int TotalUpdates { get { return _TotalUpdates; } }

        public SerialPotentiostat()
        {
            _Buffer = 0;
            _TotalUpdates = 0;
            Command = null;
            reallyconnected = false;
            simconnected = false;
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
            Connected?.Invoke(this, new EventArgs());
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
            return "OK";
        }
        private void ReceivingLoop()
        {
            var rx = new Regex("m(\\d+)u(\\d+)E(\\d+)I(\\d+)");
            Port.DiscardInBuffer();
            int fails = 0;
            Port.ReadTimeout = 10000;
            reallyconnected = true;
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
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(5);
                    }
                    _Buffer = Port.BytesToRead;
                } while (!CancelListen.IsCancellationRequested);
                Disconnected?.Invoke(this, new DisconnectedEventArgs("Cancel"));
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs("Exception: " + ex.Message));
            } finally
            {
                Port?.Close();
                reallyconnected = false;
            }
        }
        private void SimReceivingLoop(int delay)
        {
            DateTime starttime = DateTime.Now;
            do
            {
                var thisE = (int)(1023 * Math.Sin(DateTime.Now.Millisecond / 1000.0 * 2 * Math.PI));
                var thisI = (int)(1023 * Math.Cos(DateTime.Now.Millisecond / 1000.0 * 2 * Math.PI));
                var milli = (ulong)((DateTime.Now - starttime).TotalMilliseconds);
                NewData?.Invoke(this, new NewDataEventArgs(milli, 0, thisE, thisI));
                _Buffer = 123;
                System.Threading.Thread.Sleep(delay);
            } while (!CancelListen.IsCancellationRequested);
            Disconnected?.Invoke(this, new DisconnectedEventArgs("Cancel"));
        }

    }
}
