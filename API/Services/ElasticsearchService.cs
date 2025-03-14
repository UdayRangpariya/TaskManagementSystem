using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using tmp;

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
            var createIndexResponse = await _client.Indices.CreateAsync<t_tasks>(index => index
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

    public async Task IndexTaskAsync(t_tasks task)
    {
        var response = await _client.IndexAsync(task, idx => idx
            .Index(_indexName)
            .Id(task.c_task_id.ToString()) // Use task_id as document ID
            .Refresh(Elastic.Clients.Elasticsearch.Refresh.True) // Ensure immediate visibility
        );

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Failed to index task: {response.DebugInformation}");
            throw new Exception($"Failed to index task: {response.DebugInformation}");
        }
    }
    public async Task<List<t_tasks>> SearchTaskAsync(string name)
    {
        var response = await _client.SearchAsync<t_tasks>(search => search
            .Index(_indexName)  // Explicitly specify the index
            .Query(q => q
                .MatchPhrasePrefix(mp => mp
                    .Field(f => f.c_title) // Match the title field
                    .Query(name) // Search query (user's input)
                )
            )
        );

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"ElasticSearch query failed: {response.DebugInformation}");
            return new List<t_tasks>();
        }

        return response.Documents.ToList();
    }



    public async Task DeleteTaskAsync(int taskId)
    {
        try
        {
            // Attempt to delete the document by its ID
            var response = await _client.DeleteAsync<t_tasks>(taskId.ToString(), d => d.Index(_indexName));

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

    public async Task UpdateTaskAsync(t_tasks task)
    {
        try
        {
            // Prepare the update request
            var updateResponse = await _client.UpdateAsync<t_tasks, t_tasks>(task.c_task_id.ToString(), update => update
                .Index(_indexName)  // Specify the index
                .Doc(task)  // The task document to update
                .Refresh(Elastic.Clients.Elasticsearch.Refresh.True) // Ensure immediate visibility
            );

            if (updateResponse.IsValidResponse)
            {
                Console.WriteLine($"Task with ID {task.c_task_id} updated successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to update task with ID {task.c_task_id}: {updateResponse.DebugInformation}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while updating task with ID {task.c_task_id}: {ex.Message}");
        }
    }

}
