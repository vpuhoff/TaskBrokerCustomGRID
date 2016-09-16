using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace iTask
{
    public interface iTask
    {
        object DoWork(object input);
    }
}
