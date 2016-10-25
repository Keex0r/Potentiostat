using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    class VoltageDivider
    {
        public VoltageDivider()
        {
            RInToMiddle = 100;
            RMiddleToRef = 100;
            VRef = 5;
        }
        public VoltageDivider(double VRef,double RInToMiddle, double RMiddleToRef)
        {
            this.VRef = VRef;
            this.RInToMiddle = RInToMiddle;
            this.RMiddleToRef = RMiddleToRef;
        }
        public double RInToMiddle { get; set; }
        public double RMiddleToRef { get; set; }
        public double VRef { get; set; }
        public double GetVOut(double VIn)
        {
            return VIn-(VIn-VRef) * RInToMiddle / (RMiddleToRef + RInToMiddle);
        }
        public double GetVIn(double VOut)
        {
            return (VOut - VRef * RInToMiddle / (RMiddleToRef + RInToMiddle)) / (1 - RInToMiddle / (RMiddleToRef + RInToMiddle));
        }
    }
}
