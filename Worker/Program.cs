using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBroker;

namespace Worker
{
    class Program
    {
        static void Main(string[] args)
        {
        ret1: JobWorker jw = new JobWorker();
            Console.ReadLine();
            goto ret1;
        }
    }
}
