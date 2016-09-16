using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTask;
using System.Threading;
using System.Reflection;


namespace SampleTask
{
    public class Task:iTask.iTask
    {
        
        public object DoWork(object input)
        {
            int inpstr = (int)input;
            Thread.Sleep(inpstr/10);
            return inpstr;
        }
    }
}
