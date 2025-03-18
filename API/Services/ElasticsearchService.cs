using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Repositories.Model.AdminModels;
using tmp;


namespace API.Services
{
    public class ElasticsearchService
    {
        private readonly ElasticsearchClient _client;
        private string _indexName;
        public ElasticsearchService(IConfiguration configuration, ElasticsearchClient client)
        {
            _indexName = configuration["Elasticsearch:DefaultIndex"];
            _client = client;
        }
        public async Task<int> CreateIndexAsync()
        {
            var indexExistsResponse = await _client.Indices.ExistsAsync(_indexName);
            if (!indexExistsResponse.Exists)
            {
                var createIndexResponse = await _client.Indices.CreateAsync<TaskModel>(index => index
                    .Index(_indexName)
                    .Mappings(mappings => mappings
                        .Properties(properties => properties
                            .IntegerNumber(x => x.c_task_id) // Integer for task ID
                            .Text(x => x.c_title) // Text for title (searchable)
                            .Text(x => x.c_description) // Text for description (searchable)
                            .Keyword(x => x.c_status) // Keyword for task status (string values like "pending")
                            .IntegerNumber(x => x.c_priority) // Integer for priority (1-5 range)
                            .Date(x => x.c_due_date) // Date for due date
                            .Date(x => x.c_created_at) // Date for creation timestamp
                            .Date(x => x.c_updated_at) // Date for update timestamp
                            .IntegerNumber(x => x.c_created_by) // Integer for user ID (creator)
                            .IntegerNumber(x => x.c_assigned_to) // Integer for user ID (assigned)
                        )
                    )
                );
                if (!createIndexResponse.IsValidResponse)
                {
                    Console.WriteLine($"Failed to create index: {createIndexResponse.DebugInformation}");
                    return -1;
                }
                Console.WriteLine("Tasks index created successfully.");
                return 1;
            }
            else
            {
                Console.WriteLine("Tasks index already exists.");
                return 0;
            }
        }

        public async Task IndexTaskAsync(TaskModel task)
        {
            if (task.c_task_id == 0)
            {
                throw new Exception("Task ID cannot be 0");
            }

            var response = await _client.IndexAsync(task, idx => idx
                .Index(_indexName)
                .Id(task.c_task_id.ToString()) // Ensure the task ID is set
                .Refresh(Elastic.Clients.Elasticsearch.Refresh.True)
            );

            if (!response.IsValidResponse)
            {
                Console.WriteLine($"Failed to index task: {response.DebugInformation}");
                throw new Exception($"Failed to index task: {response.DebugInformation}");
            }
        }


        public async Task<List<TaskModel>> SearchTasksByTitleAndUserIdAsync(string title, int userId)
        {
            // Perform a search query where we match the task title and filter by assigned user ID
            var response = await _client.SearchAsync<TaskModel>(search => search
                .Index(_indexName)  // Explicitly specify the index
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MatchPhrasePrefix(mp => mp // Use MatchPhrasePrefix for partial matching
                                .Field(f => f.c_title) // Match on the task title
                                .Query(title)  // The title to search for
                            )
                        )
                        .Filter(f => f
                            .Term(t => t
                                .Field(fld => fld.c_assigned_to) // Filter by assigned user ID
                                .Value(userId)  // The user ID to filter by
                            )
                        )
                    )
                )
            );

            // Check for a valid response
            if (!response.IsValidResponse)
            {
                Console.WriteLine($"ElasticSearch query failed: {response.DebugInformation}");
                return new List<TaskModel>();
            }

            // If no documents are found, return an empty list
            if (!response.Documents.Any())
            {
                Console.WriteLine("No tasks found.");
                return new List<TaskModel>();
            }

            // Return the list of tasks that match the title and are assigned to the user
            return response.Documents.ToList();
        }





        public async Task DeleteTaskAsync(int taskId)
        {
            try
            {
                // Attempt to delete the document by its ID
                var response = await _client.DeleteAsync<TaskModel>(taskId.ToString(), d => d.Index(_indexName));

                if (response.IsValidResponse)
                {
                    Console.WriteLine($"Task with ID {taskId} deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to delete task with ID {taskId}: {response.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while deleting task with ID {taskId}: {ex.Message}");
            }
        }

        public async Task UpdateTaskAsync(TaskModel task)
        {
            try
            {
                var response = await _client.IndexAsync(task, idx => idx
                    .Index(_indexName)
                    .Id(task.c_task_id.ToString())
                    .Refresh(Elastic.Clients.Elasticsearch.Refresh.True)
                );

                if (response.IsValidResponse)
                {
                    Console.WriteLine($"Task with ID {task.c_task_id} updated successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to update task with ID {task.c_task_id}: {response.DebugInformation}");
                    throw new Exception($"Failed to update task: {response.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while updating task with ID {task.c_task_id}: {ex.Message}");
                throw;
            }
        }

    }
}
