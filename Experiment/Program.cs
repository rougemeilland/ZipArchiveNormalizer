using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Experiment
{
    class Program
    {
        static void Main(string[] args)
        {

            checked
            {
                var x = (UInt64)uint.MaxValue;
                var y = int.MinValue;
                Console.WriteLine(x - (UInt64)(-(Int64)y));
            }


            unchecked
            {
                Console.WriteLine((UInt64.MaxValue + (UInt64)Int64.MinValue).ToString("x"));
            }
            checked
            {
                var x = 0L;
                Console.WriteLine(string.Format("-({0}) == {1}", x, Negate(x)));
            }

            checked
            {
                var x = 3L;
                Console.WriteLine(string.Format("-({0}) == {1}", x, Negate(x)));
            }

            checked
            {
                var x = long.MaxValue;
                Console.WriteLine(string.Format("-({0}) == {1}", x, Negate(x)));
            }

            checked
            {
                var x = long.MinValue;
                Console.WriteLine(string.Format("-({0}) == {1}", x, Negate(x)));
            }


            Console.WriteLine("OK");
            Console.Beep();
            Console.ReadLine();
        }

        private static long Negate(long x)
        {
            checked
            {
                if (x >= 0)
                    return  -x;
                else if (x != long.MinValue)
                     return -x;
                else
                    return (-(x + 1)) + 1;
            }
        }
    }
}