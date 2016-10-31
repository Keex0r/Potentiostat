using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    public class Misc
    {
        public static string NumberToString(double v, string Unit)
        {
            if (Math.Abs(v) < 1e-9)
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
                return v.ToString("0.000") + " " + Unit;
            }
        }

        public struct RawDataPoint
        {
            public double Time;
            public int E;
            public int I;
            public RawDataPoint(ulong Milli, ulong Micro, int E, int I)
            {
                this.Time = (Milli + Micro / 1000.0)/1000.0;
                this.E = E;
                this.I = I;
            }
            public RawDataPoint(double Time, int E, int I)
            {
                this.Time = Time;
                this.E = E;
                this.I = I;
            }
        }
    }
}
