using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running TFTP server");
            Console.WriteLine();
            Console.WriteLine("Press any key to close the server.");
            
            //need to fix this, I don't think server will ever stop
            Server server = new Server(69);
            Console.Read();
        }
    }
}
