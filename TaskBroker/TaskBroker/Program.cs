using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            var bytes = File.ReadAllBytes(@"C:\Users\vipuhov\Desktop\Разработки\TaskBroker\SampleTask\bin\Debug\SampleTask.dll");
            //var bytes = File.ReadAllBytes(@"C:\Users\vipuhov\Desktop\Разработки\glgdi\binaries\OpenTK.dll");
            sw.Start();
            for (int i = 0; i < 1000; i++)
            {
                var s = (string)DoWorkTask("tester",bytes);
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds.ToString());
            Console.ReadLine();
        }

        static object DoWorkTask(object data, byte[] rawdll)
        {
            var asm = Assembly.Load(rawdll);
            var types = asm.GetTypes();
            foreach (var type in types)
            {
                if (type.Name == "Task")
                {
                    var runnable = Activator.CreateInstance(type) as iTask.iTask;
                    if (runnable == null) throw new Exception("broke");
                    var result = runnable.DoWork("Test!");
                    runnable = null;
                    types = null;
                    asm = null;
                    return result;
                }
                break;
            }
            types = null;
            asm = null;
            GC.Collect();
            return null;
        }
    }
}
