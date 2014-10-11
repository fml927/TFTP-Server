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
            Console.WriteLine("Running TFTP server on port 69 for directory: " + Environment.CurrentDirectory);
            Console.WriteLine();
            Console.WriteLine("Press any key to close the server.");

            //new thread so UI isn't blocked
            Thread t = new Thread(() => new Server());

            //so thread closes with applcation
            t.IsBackground = true;
            t.Start();

            //exit application
            Console.Read();
        }
    }
}
