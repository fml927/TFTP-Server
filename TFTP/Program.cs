using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TFTP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running TFTP server for directory: " + Environment.CurrentDirectory);
            Console.WriteLine();
            Console.WriteLine("Press any key to close the server.");

            Thread t = new Thread(() => new Server());
            t.IsBackground = true;
            t.Start();
            Console.Read();
        }
    }
}
