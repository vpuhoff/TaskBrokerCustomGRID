using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    [Serializable]
    public class Job
    {
        public string ID = Guid.NewGuid().ToString();
        public Object JobData;
        public Object Result;
        public JobStatus Status;
        public string Message;
        public string WorkerID;
    }

    [Serializable]
    public enum JobStatus
    {
        OK,
        ERROR,
        NoWork,
        NewJob
    }
}
