using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TaskBroker;

namespace remoteclass
{
    public class XX : MarshalByRefObject
    {
        public bool Hello(string text)
        {
            Console.WriteLine(text);
            return true;
        }

        public List<Job> JobsToWork = new List<Job>();
        public List<Job> InWork = new List<Job>();
        public List<Job> Completed = new List<Job>();
        public List<Job> Errors = new List<Job>();

        int jobscomplete = 0;
        int sendedjobs = 0;
        int returnedcount = 0;
        int errorscount = 0;

        byte[] ExecutorData;

        public byte[] GetExecutor()
        {
            return ExecutorData;
        }

        public bool SaveExecutor(byte[] data)
        {
            ExecutorData = data;
            return true;
        }

        public bool PutJobsToWork(List<Job> jobs)
        {
            lock (JobsToWork)
            {
                foreach (var item in jobs)
                {
                    JobsToWork.Add(item);
                }
            } 
            return true;
        }

        public int GetJobsCount()
        {
            return JobsToWork.Count + InWork.Count + Completed.Count;
        }

        public List<Job> GetCompleted()
        {
            List<Job> jobs=null ;
            lock (Completed )
            {
                if (Completed.Count >0)
                {
                    jobs = new List<Job>();
                    foreach (var item in Completed)
                    {
                        jobs.Add(item);
                    }
                    Completed.Clear();
                }
            } 
            return jobs;
        }

        public Job GetJobFree()
        {
            Job jb = new Job();
            jb.Status = JobStatus.NoWork;
            return jb;
        }
        Random rnd = new Random();

        public Job GetJob()
        {
            Job job;
            lock (JobsToWork )
            {
                if (JobsToWork.Count > 0)
                {
                    job = JobsToWork[0];
                    JobsToWork.RemoveAt(0);
                    lock (InWork)
                    {
                        InWork.Add(job);
                    }
                }
                else
                {
                    //lock (InWork) //раздавать задачи, которыми уже занимаются тем кто не занят, может быстрей сделают
                    //{
                    //    if (InWork.Count>0)
                    //    {
                    //        int n = rnd.Next(InWork.Count - 1);
                    //        var job = InWork[n];
                    //        return job;
                    //    }
                    //    else
                    //    {
                    //        return GetJobFree();
                    //    }
                    //}
                    job= GetJobFree();
                }
            }
            return job;
        }
        public void SaveResult(Job result)
        {
            if (result.Status == JobStatus.OK )
            {
                jobscomplete++;
                lock (InWork )
                {
                    foreach (var item in InWork)
                    {
                        if (item.ID == result.ID)
                        {
                            InWork.Remove(item);
                            break;
                        }
                    }
                }
                lock (Completed )
                {
                    Completed.Add(result);
                }
            }
            else
            {
                if (result.Status == JobStatus.ERROR )
                {
                    lock (Errors)
                    {
                        Errors.Add(result);
                    }
                    errorscount++;
                }
            }
            returnedcount++;
            Console.Clear();
            Console.WriteLine("errorscount:\t" + errorscount.ToString());
            Console.WriteLine("jobscomplete:\t" + jobscomplete.ToString());
            Console.WriteLine("sendedjobs:\t" + sendedjobs.ToString());
            Console.WriteLine("returnedcount:\t" + returnedcount.ToString());
        }

        public int GetServiceCode()
        {
            return this.GetHashCode();
        }


        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr handle, int minimumWorkingSetSize, int maximumWorkingSetSize);
        public static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
        }

        [DllImport("kernel32.dll")]
        public static extern void ExitProcess([In] uint uExitCode);



        public String ByteToString(byte[] byteArray)
        {
            return Convert.ToBase64String(byteArray);
        }

        public string DeviceID { get; set; }
    }
}
