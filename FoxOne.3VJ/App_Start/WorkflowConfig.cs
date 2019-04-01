using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne._3VJ;
using System.Web;
using FoxOne.Workflow.Kernel;
using FoxOne.Core;

[assembly: PreApplicationStartMethod(typeof(WorkflowConfig), "Register")]

namespace FoxOne._3VJ
{
    public class WorkflowConfig
    {
        public static void Register()
        {

        }
    }
}
