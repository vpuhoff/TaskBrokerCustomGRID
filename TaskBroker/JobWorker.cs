using multiCastRecv;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskBroker
{
   public class JobWorker
    {
       int maxworkers = 30;


       public JobWorker(string castgroup = "224.0.0.0", int udpport = 5000)
        {
            var CastListener = new CastRecv(castgroup, udpport.ToString());
            CastListener.onNewMessage += CastListener_onNewMessage;
            Console.WriteLine("Press ENTER to EXIT");
            Console.ReadLine();
            // using TCP protocol
        }

        byte[] runtimedata = new byte[1];
        static Dictionary<string, iTask.iTask> Cache = new Dictionary<string, iTask.iTask>();

        Thread GetWorker(string ip,int port)
        {
            return new Thread((n =>
            {
                string WorkerID = Guid.NewGuid().ToString();
                Console.WriteLine("Started Client :" + Guid.NewGuid().ToString());
                var remoteObject = (remoteclass.XX)Activator.GetObject(typeof(remoteclass.XX), String.Format("tcp://{0}:{1}/getJob", ip, port));
                lock (runtimedata)
                {
                    if (runtimedata.Length ==1)
                    {
                        runtimedata = remoteObject.GetExecutor();
                    }
                }
                Job job = null;
            ret1: try
                {
                    Console.Write(".");
                    job = remoteObject.GetJob();
                    if (job.Status != JobStatus.NoWork)
                    {
                        job.WorkerID = WorkerID;
                        job.Result = job.JobData;
                        job.Status = JobStatus.OK;
                        DoJob(job, WorkerID);
                        remoteObject.SaveResult(job);
                        goto ret1;
                    }
                }
                catch (Exception e)
                {
                    if (job != null)
                    {
                        job.Status = JobStatus.ERROR;
                        job.Message = e.Message + "\r\n" + e.StackTrace + "\r\n" + e.Source;
                        try
                        {
                            remoteObject.SaveResult(job);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Ошибка при отправке результата:" + e.Message);
                        }
                    }
                    Console.WriteLine("Error:" + e.Message);
                    goto ret1;
                }
                Console.WriteLine("Completed all jobs... Exiting worker...");
            }));
        }


        void ReadMessage(string message)
        {
            if (message.Contains(":"))
            {
                var mes = message.Split(':');
                if (mes.Length==3)
                {
                    string ip = mes[0];
                    int port = int.Parse(mes[1]);
                    string jobid = mes[2];
                    //тут нужно еще проверить соединение с сервером
                    var remoteObject = (remoteclass.XX)Activator.GetObject(typeof(remoteclass.XX), String.Format("tcp://{0}:{1}/getJob", ip, port));
                    int retmax = 10;
                    int retcnt = 0;
                ret1: try
                    {
                        if (remoteObject.Hello("Hello") == true)
                        {
                            CleanWorkers();
                            for (int i = 0; i < maxworkers; i++)
                            {
                                if (workers.Count < maxworkers)//тут можно "умные условия для запуска поставить"
                                {
                                    var worker = GetWorker(ip, port);
                                    worker.Priority = ThreadPriority.Lowest;
                                    worker.Start();
                                    workers.Add(worker);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        retcnt++;
                        if (retcnt>10)
                        {
                            return;
                        }
                        else
                        {
                            goto ret1;
                        }
                        Random rnd = new Random(DateTime.Now.Millisecond);
                        Thread.Sleep(rnd.Next(10));
                    }
                }       
            }
        }

        void CleanWorkers()
        {
            for (int i = workers.Count-1 ; i >=0; i--)
			{
			  if (workers[i].ThreadState == ThreadState.Stopped)
                {
                    workers.RemoveAt(i);
                }
			}
        }
        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
        List<Thread> workers = new List<Thread>();

        void CastListener_onNewMessage(byte[] data)
        {
            try
            {
                var Message = GetString(data);
                if (Message != null)
                {
                    ReadMessage(Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка распаковки сообщения: "+e.Message+"\r\n"+e.StackTrace+"\r\n"+e.Source );
            }
        }

        Job DoJob(Job  job,string workerid)
        {
            try
            {
                var result = DoWorkTask(job.JobData, ref runtimedata, workerid);
                job.Status = JobStatus.OK;
                job.Result = result;
                return job;
            }
            catch (Exception e)
            {
                job.Status = JobStatus.ERROR ;
                job.Message = e.Message + e.Source + e.StackTrace + e.TargetSite;
                return job;
            }
        }

        Dictionary<string, Assembly> AssCache = new Dictionary<string, Assembly>();
        object DoWorkTask(object data, ref byte[] rawdll, string workerid)
        {
            string vers = GetVersionFast(rawdll);
            if (Cache.ContainsKey(vers + workerid))
            {
                var runnable = Cache[vers + workerid];
                if (runnable == null) throw new Exception("broke");
                var result = runnable.DoWork(data);
                return result;
            }
            Assembly asm;
            lock (AssCache)
            {
                if (AssCache.ContainsKey(vers))
                {
                    asm = AssCache[vers];
                }
                else
                {
                    asm = Assembly.Load(rawdll);
                }
            }
            var types = asm.GetTypes();
            foreach (var type in types)
            {
                if (type.Name == "Task")
                {
                    var runnable = Activator.CreateInstance(type) as iTask.iTask;
                    if (runnable == null) throw new Exception("broke");
                    var result = runnable.DoWork(data);
                    Cache.Add(vers + workerid, runnable);
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

        static string GetVersionFast(byte[] rawdll)
        {
            string name = "";
            string result = System.Text.Encoding.UTF8.GetString(rawdll);
            result = result.Replace("\0", "");
            result = result.Substring(result.IndexOf("Assembly Version"));
            foreach (char c in result)
            {
                if (Char.IsDigit(c) | c == '.')
                {
                    name += c;
                }
            }
            return name;
        }
    }
}
