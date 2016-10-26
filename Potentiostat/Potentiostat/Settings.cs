using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    class Settings
    {
        public Settings()
        {
            Shunts = new BindingList<Shunt>();
            AddShunts();
            Vp = 4.91;
            Vn = -4.75;
            CurrentSenseDivider = new VoltageDivider(Vp, 10000.0, 9980.0);
            WEVoltSenseDivider = new VoltageDivider(Vp, 9980.0, 9970.0);
            Averaging = 100;
        }

        private void AddShunts()
        {
            Shunts.Add(new Shunt("R2", 100000.0, false));
            Shunts.Add(new Shunt("R3", 10070.0, true));
            Shunts.Add(new Shunt("R4", 995.5,false));
            Shunts.Add(new Shunt("R5", 99.6, false));
        }

        public double GetShuntResistance()
        {
            return Shunts.Where(s => s.Active).Select(s => s.Resistance).Sum();
        }

        public BindingList<Shunt> Shunts;
        public double Vp;
        public double Vn;
        public VoltageDivider CurrentSenseDivider;
        public VoltageDivider WEVoltSenseDivider;
        public int Averaging;
        public double VoltageCalibm;
        public double VoltageCalibb;

        public Tuple<double,double> GetCurrentRange()
        {
            var Resi = GetShuntResistance();
            var In = Vn / Resi;
            var Ip = Vp / Resi;
            return Tuple.Create(In, Ip);
        }
        public double GetCurrentResolution()
        {
            var CurrentRange = GetCurrentRange();
            var factor = Vp/(Vp - Vn);
            return (CurrentRange.Item2 - CurrentRange.Item1) / 1023.0 / factor;
        }
        public double GetVoltage(int AnalogValue)
        {
            var E = AnalogValue / 1023.0 * Vp;
            var nominal= -WEVoltSenseDivider.GetVIn(E);
            return VoltageCalibm * nominal + VoltageCalibb;
        }
        public double GetCurrent(int AnalogValue)
        {
            var E = AnalogValue / 1023.0 * Vp;
            var Ereal = CurrentSenseDivider.GetVIn(E);
            return Ereal / GetShuntResistance();

        }
    }
}
