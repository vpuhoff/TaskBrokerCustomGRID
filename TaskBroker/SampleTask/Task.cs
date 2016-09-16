using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTask;
using System.Threading;

namespace SampleTask
{
    public class Task:iTask.iTask
    {
        public object DoWork(object input)
        {
            string inpstr = (string)input;
            Thread.Sleep(10);
            inpstr += inpstr;
            return inpstr;
        }
    }
}
