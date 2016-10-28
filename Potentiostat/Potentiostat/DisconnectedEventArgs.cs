using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    class DisconnectedEventArgs
    {
        public string Reason;
        public DisconnectedEventArgs(string Reason)
        {
            this.Reason = Reason;
        }
    }
}
