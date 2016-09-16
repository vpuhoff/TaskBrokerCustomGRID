using multiCastRecv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    class Program
    {
        static Stopwatch sw;
        static int NeedWorks = 0;
        static int Completed = 0;
        static Random rnd = new Random();

        static void Main(string[] args)
        {
            sw= new Stopwatch(); sw.Start(); //Замеряем время инициализации сервера
            List<Job> jobs = new List<Job>();//Формируем тестовые "задания" 
            for (int i = 0; i < 1000; i++)
            {
                Job nj = new Job();
                nj.Status = JobStatus.NewJob;
                nj.JobData = rnd.Next(3000);
                NeedWorks += (int)nj.JobData;
                jobs.Add(nj);
            }
            string dllfile = @"SampleTask.dll";
            int freeport = FreeTcpPort();
            JobBroker jb = new JobBroker(freeport, jobs, dllfile,freeport );//Запускаем брокер
            jb.onCompleteJob += jb_onCompleteJob;//подписываемся на результаты обработки
            jb.onCompleteAll += jb_onCompleteAll;
            Console.ReadLine();
        }

        static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        static void jb_onCompleteAll()
        {
            sw.Stop();
            Console.Clear();
            Console.WriteLine("Брокер выполнил все поставленные задачи за " + sw.ElapsedMilliseconds.ToString() + " мс.");
            Console.WriteLine("Всего было запланировано задач на " + NeedWorks.ToString() + " мс.");
            Console.WriteLine("Результатов возвращено всего: " + ResultCount.ToString());
        }

        static int ResultCount = 0;
        static void jb_onCompleteJob(Job job)
        {
            ResultCount++;
            if (job.Status== JobStatus.OK )
            {
                Completed += (int)job.Result;
                Console.WriteLine(job.Message);
            }
            Console.Write("*");
            Console.WriteLine("Выполнено: " + Completed.ToString()+" из "+NeedWorks.ToString() + " мс.");
        }

        

    }
}
