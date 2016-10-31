using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    public class SingleShotEventArgs
    {
        public List<Misc.RawDataPoint> Data;
        public SingleShotEventArgs(List<ulong> Time, List<int> E, List<int> I)
        {
            Data = new List<Misc.RawDataPoint>();
            for(int i=0;i<Time.Count();i++)
            {
                Data.Add(new Misc.RawDataPoint(Time[i] / 1e6, E[i], I[i]));
            }
        }
    }
}
