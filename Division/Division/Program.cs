using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Division
{
    class Program
    {
        static void Main(string[] args)
        {
            unsafe
            {
                //18446744073709551
                for (ulong r=0;r<20;r++)
                {
                    ulong milli = ulong.MaxValue/1000;
                    ulong micros = 100-milli*1000;
                    
                    Console.WriteLine(micros);
                    Console.WriteLine(milli);
                    Console.ReadKey();
                }
                
            }
           
        }
    }
}
