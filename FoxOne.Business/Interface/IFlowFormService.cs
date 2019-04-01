using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business
{
    public interface IFlowFormService
    {
        bool CanRunFlow();

        IDictionary<string, object> SetParameter();

        void OnFlowFinish(string instanceId, string dataLocator, bool agree, string denyOption);
    }
}
