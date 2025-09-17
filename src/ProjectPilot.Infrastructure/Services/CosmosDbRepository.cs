using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;
using ProjectPilot.Infrastructure.Configuration;
using System.Net;

namespace ProjectPilot.Infrastructure.Services;

public class CosmosDbRepository<T> : IRepository<T> where T : class
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbRepository<T>> _logger;

    public CosmosDbRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbOptions> options,
        ILogger<CosmosDbRepository<T>> logger)
    {
        var cosmosOptions = options.Value;
        _container = cosmosClient.GetContainer(cosmosOptions.DatabaseName, GetContainerName(typeof(T)));
        _logger = logger;
    }

    public async Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Item with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item with ID {Id}", id);
            throw;
        }
    }

    public async Task<List<T>> GetAllAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c OFFSET @skip LIMIT @take")
                .WithParameter("@skip", skip)
                .WithParameter("@take", take);

            var iterator = _container.GetItemQueryIterator<T>(query);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all items");
            throw;
        }
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.CreateItemAsync(entity, cancellationToken: cancellationToken);
            _logger.LogInformation("Item added to Cosmos DB with ID {Id}", GetEntityId(entity));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to Cosmos DB");
            throw;
        }
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.UpsertItemAsync(entity, cancellationToken: cancellationToken);
            _logger.LogInformation("Item updated in Cosmos DB with ID {Id}", GetEntityId(entity));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item in Cosmos DB");
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            _logger.LogInformation("Item deleted from Cosmos DB with ID {Id}", id);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Item with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item with ID {Id}", id);
            throw;
        }
    }

    private static string GetContainerName(Type type)
    {
        return type.Name switch
        {
            nameof(MeetingTranscription) => "Transcriptions",
            nameof(TaskItem) => "Tasks",
            _ => type.Name + "s"
        };
    }

    private static string GetEntityId(T entity)
    {
        // Try to get ID from common properties
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}