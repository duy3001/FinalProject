using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
        int GetVectorSize();
    }
}

