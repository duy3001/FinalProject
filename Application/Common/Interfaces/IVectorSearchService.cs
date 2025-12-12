using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IVectorSearchService
    {
        Task<bool> UpsertPointAsync(string collectionName, string pointId, float[] vector, Dictionary<string, object> payload, CancellationToken ct = default);
        Task<bool> DeletePointAsync(string collectionName, string pointId, CancellationToken ct = default);
        Task<List<SearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, CancellationToken ct = default);
        Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default);
        Task<bool> CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default);
    }

    public class SearchResult
    {
        public string Id { get; set; } = default!;
        public float Score { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new();
    }
}

