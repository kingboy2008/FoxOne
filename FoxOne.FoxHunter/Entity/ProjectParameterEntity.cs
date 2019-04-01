using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Core;
using FoxOne.Data.Attributes;

namespace FoxOne.FoxHunter
{
    [Table("proj_project_parameter")]
    public class ProjectParameterEntity
    {
        public string Id { get; set; }

        public string ProjectId { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public string RateParam { get; set; }

        public string CountParam { get; set; }

        public string RateQuery { get; set; }

        public string CountQuery { get; set; }

        public string OtherParam { get; set; }

        public string Calculator { get; set; }

        public string Limit { get; set; }

        private CalculatorBase calculatorInstance;

        public CalculatorBase CalculatorInstance
        {
            get
            {
                if (calculatorInstance == null)
                {
                    calculatorInstance= Activator.CreateInstance(TypeHelper.GetType(this.Calculator)) as CalculatorBase;
                }
                return calculatorInstance;
            }
        }
    }
}
