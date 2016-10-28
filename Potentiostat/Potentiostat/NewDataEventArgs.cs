using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    class NewDataEventArgs : EventArgs
    {
        public NewDataEventArgs(ulong millis, ulong micros, int Potential, int Current)
        {
            this.millis = millis;
            this.micros = micros;
            this.Potential = Potential;
            this.Current = Current;
        }
        public ulong millis;
        public ulong micros;
        public int Potential;
        public int Current;
    }
}
