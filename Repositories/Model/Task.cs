using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace tmp;
public enum task_status
{
    pending = 1 ,
    in_progress = 2 ,
    completed = 3
    
}


public class t_tasks
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int c_task_id { get; set; }

    [Required]
    [StringLength(100)]
    public string c_title { get; set; }

    public string c_description { get; set; }

    [Required]
    [EnumDataType(typeof(task_status))]
    public task_status c_status { get; set; } = task_status.pending;

    [Range(1, 5)]
    public int? c_priority { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? c_due_date { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime c_created_at { get; set; } = DateTime.UtcNow;

    [DataType(DataType.DateTime)]
    public DateTime c_updated_at { get; set; } = DateTime.UtcNow;

    public int? c_created_by { get; set; }

    public int? c_assigned_to { get; set; }
}
