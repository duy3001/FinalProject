using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateAnswerAsync(string question, string context, CancellationToken ct = default);
    }
}

