using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Data.Attributes;

namespace FoxOne.FoxHunter
{
    [Table("proj_worktime")]
    public class WorkTimeEntity
    {
        public string Id { get; set; }

        public string ProjectId { get; set; }

        public string WorkItemId { get; set; }

        public string UserId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Description { get; set; }

        public string Question { get; set; }

        public string CheckerId { get; set; }

        public DateTime CheckTime { get; set; }

        public WorktimeStatus Status { get; set; }
    }
}
