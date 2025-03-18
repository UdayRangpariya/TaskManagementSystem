namespace Repositories.Model
{
    public class SystemStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalMessages { get; set; }
        public int UnreadMessages { get; set; }
    }
}