using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskBroker
{
    public class JobBroker
    {
        public delegate void onCompleteJobContainer(Job job);
        public event onCompleteJobContainer onCompleteJob;

        public delegate void onCompleteAllContainer();
        public event onCompleteAllContainer onCompleteAll;

        bool stopBroker = false;
        Stack<Job> Completed = new Stack<Job>();
        remoteclass.XX remoteObject;

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public JobBroker(int TCP_port,List<Job> jobs, string ExecuterDLL, int brokerport,string multicastgroup="224.0.0.0",int multicastport=5000, string brokerip="127.0.0.1")
        {
            new Thread((n =>//start job broker remote server
            {
                Stopwatch sw = new Stopwatch(); sw.Start();
                TcpChannel ch = new TcpChannel(TCP_port);
                ChannelServices.RegisterChannel(ch, false);
                RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.On;
                RemotingConfiguration.RegisterWellKnownServiceType(typeof
                               (remoteclass.XX), "getJob", WellKnownObjectMode.Singleton);
                Console.Write("Server is Ready........ Send jobs to server...");
                remoteObject = (remoteclass.XX)Activator.GetObject(typeof(remoteclass.XX), String.Format("tcp://127.0.0.1:{0}/getJob", TCP_port));
                remoteObject.PutJobsToWork(jobs);
                Console.Write("Jobs sent complete... Send Executor... ");
                remoteObject.SaveExecutor(File.ReadAllBytes(ExecuterDLL));
                Console.Write("Send Executor complete... Wait for workers...");
                //send broadcast message to get workers
                string cmg = brokerip+":"+brokerport+":"+Guid.NewGuid().ToString();
                byte[] CastMessage = GetBytes(cmg);
                sw.Stop();
                Console.WriteLine("Job broker started at " + sw.ElapsedMilliseconds + " ms.");
                sw.Reset(); sw.Start();

                //Ищем свободных работников 
                new multiCastSend.send((byte[])CastMessage.Clone(), multicastgroup, multicastport);
                sw.Stop();
                int i = 0;
                Console.WriteLine("Сообщение на поиск работников отправлено за " + sw.ElapsedMilliseconds + " мс.");
                do
                {
                    i++;
                    if (i%1000==0)
                    {
                        i = 0;//отправляем повторные сообщения 1 раз в минуту, может появились другие работники
                        new multiCastSend.send((byte[])CastMessage.Clone(), multicastgroup, multicastport);
                    }
                    Thread.Sleep(60);
                } while (!stopBroker);
            })).Start();
            
            Thread.Sleep(100);//wait for start server
             new Thread((n =>//check for completed tasks and get results
            {
                Stopwatch sw = new Stopwatch(); sw.Start();
                do
                {
                    var jobscount = remoteObject.GetJobsCount();
                    if (remoteObject.Completed.Count > 0)
                    {
                        var result = remoteObject.GetCompleted();
                        if (result!=null )
	                    {
		                    foreach (var item in result)
                            {
                                Completed.Push (item);
                            }
	                    }
                        if (onCompleteJob!=null )
	                    {
                            while (Completed.Count>0)
                            {
                                onCompleteJob(Completed.Pop());
                            }
	                    }
                    }
                    if (jobscount == 0)
                    {
                        //need to exit thread;
                        //break;
                    ret1: if (Completed.Count ==0)
                        {
                            stopBroker = true;
                            break;
                        }
                        else
                        {
                            if (onCompleteJob != null)
                            {
                                while (Completed.Count > 0)
                                {
                                    onCompleteJob(Completed.Pop());
                                }
                            }
                            else
                            {
                                Console.WriteLine("Need to Subscribe Complete EVENT!");
                                Thread.Sleep(10);
                                goto ret1;
                            }
                        }
                    }
                    Thread.Sleep(50);
                } while (true);
                sw.Stop();
                Console.WriteLine("All Jobs Complete at " + sw.ElapsedMilliseconds + " ms.");
                if (onCompleteAll!=null )
                {
                    onCompleteAll();    
                }
            })).Start();
        }
    }
}
