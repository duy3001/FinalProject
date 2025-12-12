using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class QdrantService : IVectorSearchService
    {
        private readonly QdrantClient _client;
        private readonly string _defaultCollectionName;

        public QdrantService(IConfiguration configuration)
        {
            var host = configuration["Qdrant:Host"] ?? "localhost";
            var port = int.Parse(configuration["Qdrant:Port"] ?? "6333");
            var apiKey = configuration["Qdrant:ApiKey"];

            var clientOptions = new QdrantClientOptions
            {
                Host = host,
                Port = port
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                clientOptions.ApiKey = apiKey;
            }

            _client = new QdrantClient(clientOptions);
            _defaultCollectionName = configuration["Qdrant:DefaultCollection"] ?? "answers";
        }

        public async Task<bool> UpsertPointAsync(string collectionName, string pointId, float[] vector, Dictionary<string, object> payload, CancellationToken ct = default)
        {
            try
            {
                var points = new List<PointStruct>
                {
                    new PointStruct
                    {
                        Id = pointId,
                        Vectors = vector,
                        Payload = { ConvertPayload(payload) }
                    }
                };

                await _client.UpsertAsync(collectionName, points, cancellationToken: ct);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upserting point to Qdrant: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeletePointAsync(string collectionName, string pointId, CancellationToken ct = default)
        {
            try
            {
                var pointIds = new List<string> { pointId };
                await _client.DeleteAsync(collectionName, pointIds, cancellationToken: ct);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting point from Qdrant: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, CancellationToken ct = default)
        {
            try
            {
                var searchRequest = new SearchPoints
                {
                    CollectionName = collectionName,
                    Vector = queryVector,
                    Limit = (uint)limit,
                    WithPayload = new WithPayloadSelector { Enable = true }
                };

                if (filter != null && filter.Any())
                {
                    searchRequest.Filter = BuildFilter(filter);
                }

                var response = await _client.SearchAsync(searchRequest, cancellationToken: ct);

                return response.Select(r => new SearchResult
                {
                    Id = r.Id.StringValue ?? r.Id.NumValue.ToString(),
                    Score = r.Score,
                    Payload = ConvertFromQdrantPayload(r.Payload)
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching in Qdrant: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default)
        {
            try
            {
                var collections = await _client.ListCollectionsAsync(cancellationToken: ct);
                return collections.Any(c => c == collectionName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking collection existence: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default)
        {
            try
            {
                var exists = await CollectionExistsAsync(collectionName, ct);
                if (exists)
                {
                    return true;
                }

                var vectorParams = new VectorParams
                {
                    Size = (uint)vectorSize,
                    Distance = Distance.Cosine
                };

                await _client.CreateCollectionAsync(collectionName, vectorParams, cancellationToken: ct);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating collection in Qdrant: {ex.Message}");
                return false;
            }
        }

        private Dictionary<string, Value> ConvertPayload(Dictionary<string, object> payload)
        {
            var result = new Dictionary<string, Value>();
            foreach (var kvp in payload)
            {
                result[kvp.Key] = ConvertToQdrantValue(kvp.Value);
            }
            return result;
        }

        private Value ConvertToQdrantValue(object value)
        {
            return value switch
            {
                string str => new Value { StringValue = str },
                int i => new Value { IntegerValue = i },
                long l => new Value { IntegerValue = l },
                float f => new Value { DoubleValue = f },
                double d => new Value { DoubleValue = d },
                bool b => new Value { BoolValue = b },
                DateTime dt => new Value { StringValue = dt.ToString("O") },
                _ => new Value { StringValue = value?.ToString() ?? string.Empty }
            };
        }

        private Dictionary<string, object> ConvertFromQdrantPayload(Dictionary<string, Value> payload)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in payload)
            {
                result[kvp.Key] = ConvertFromQdrantValue(kvp.Value);
            }
            return result;
        }

        private object ConvertFromQdrantValue(Value value)
        {
            if (value.StringValue != null) return value.StringValue;
            if (value.IntegerValue != 0) return value.IntegerValue;
            if (value.DoubleValue != 0) return value.DoubleValue;
            if (value.BoolValue) return value.BoolValue;
            return value.ToString();
        }

        private Filter? BuildFilter(Dictionary<string, object> filter)
        {
            if (!filter.Any()) return null;

            var conditions = new List<Condition>();
            foreach (var kvp in filter)
            {
                conditions.Add(new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = kvp.Key,
                        Match = new Match
                        {
                            Value = ConvertToQdrantValue(kvp.Value)
                        }
                    }
                });
            }

            return new Filter
            {
                Must = { conditions }
            };
        }
    }
}

