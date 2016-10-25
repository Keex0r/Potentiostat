using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potentiostat
{
    class Shunt : System.ComponentModel.INotifyPropertyChanged
    {
        public Shunt()
        {
            _Name = "";
            _Resistance = 0;
            _Active = false;
        }
        public Shunt(string Name, double Resistance, bool Active)
        {
            _Name = Name;
            _Resistance = Resistance;
            _Active = Active;
        }
      
        private bool _Active;
        public bool Active
        {
            get
            {
                return _Active;
            }
            set
            {
                _Active = value;
               if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Active"));
            }
        }

        private string _Name;
        public string Name { get { return _Name; } }
        private double _Resistance;
        public double Resistance { get { return _Resistance; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public double GetCurrent(double Voltage)
        {
            return Voltage / Resistance;
        }
    }
}
