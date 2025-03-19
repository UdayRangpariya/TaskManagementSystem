using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
namespace Repositories.Model.AdminModels
{
   
    public class TaskModel
    {

        public int c_task_id { get; set; }

        public string c_title { get; set; }
    
        public string c_description { get; set; }
        public task_status c_status { get; set; } = task_status.pending;
        public int? c_priority { get; set; }
        public DateTime? c_due_date { get; set; }
        public DateTime c_created_at { get; set; } = DateTime.UtcNow;
        public DateTime c_updated_at { get; set; } = DateTime.UtcNow;
        public int? c_created_by { get; set; }
        public int? c_assigned_to { get; set; }

    }
}