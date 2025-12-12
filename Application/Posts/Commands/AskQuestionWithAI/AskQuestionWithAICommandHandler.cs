using Application.Common.Interfaces;
using Application.Posts.Commands.AskQuestionWithAI;
using Application.Posts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Posts.Handlers
{
    public class AskQuestionWithAICommandHandler : IRequestHandler<AskQuestionWithAICommand, AskQuestionWithAIResponse>
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorSearchService _vectorSearch;
        private readonly IAIService _aiService;
        private readonly IApplicationDbContext _db;
        private readonly double _defaultThreshold = 0.7;
        private readonly int _defaultMaxContext = 5;

        public AskQuestionWithAICommandHandler(
            IEmbeddingService embeddingService,
            IVectorSearchService vectorSearch,
            IAIService aiService,
            IApplicationDbContext db)
        {
            _embeddingService = embeddingService;
            _vectorSearch = vectorSearch;
            _aiService = aiService;
            _db = db;
        }

        public async Task<AskQuestionWithAIResponse> Handle(AskQuestionWithAICommand request, CancellationToken ct)
        {
            var req = request.Request;
            var question = req.Question.Trim();

            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question cannot be empty");
            }

            // Step 2: Embed the question
            var questionVector = await _embeddingService.GenerateEmbeddingAsync(question, ct);

            // Step 3: Search Qdrant
            var threshold = req.SimilarityThreshold ?? _defaultThreshold;
            var maxContext = req.MaxContextItems ?? _defaultMaxContext;

            var searchResults = await _vectorSearch.SearchAsync(
                collectionName: "answers",
                queryVector: questionVector,
                limit: maxContext * 2, // Get more results to filter by threshold
                filter: new Dictionary<string, object> { { "is_active", true } },
                ct: ct
            );

            // Step 4: Process search results (apply threshold)
            var filteredResults = searchResults
                .Where(r => r.Score >= threshold)
                .OrderByDescending(r => r.Score)
                .Take(maxContext)
                .ToList();

            // Step 5: Build context for AI
            var contextBuilder = new StringBuilder();
            var relatedQuestionIds = new HashSet<long>();

            if (filteredResults.Any())
            {
                contextBuilder.AppendLine("Các câu trả lời liên quan từ cơ sở dữ liệu:");
                contextBuilder.AppendLine();

                foreach (var result in filteredResults)
                {
                    if (result.Payload.TryGetValue("answer_text", out var answerText))
                    {
                        contextBuilder.AppendLine($"- {answerText}");
                    }

                    // Extract question_id from payload - support both UUID string and Post ID
                    // Try post_id first (if stored directly)
                    if (result.Payload.TryGetValue("post_id", out var postIdObj))
                    {
                        if (postIdObj is long postId)
                        {
                            relatedQuestionIds.Add(postId);
                        }
                        else if (postIdObj is string postIdStr && long.TryParse(postIdStr, out var pId))
                        {
                            relatedQuestionIds.Add(pId);
                        }
                    }
                    // Fallback to question_id (UUID) - need to find Post by some mapping
                    // For now, we'll try to parse as long if possible
                    else if (result.Payload.TryGetValue("question_id", out var questionIdObj))
                    {
                        if (questionIdObj is long qId)
                        {
                            relatedQuestionIds.Add(qId);
                        }
                        else if (questionIdObj is string questionIdStr)
                        {
                            // If it's a UUID string, we might need to query database to find Post
                            // For now, try parsing as long if it's numeric
                            if (long.TryParse(questionIdStr, out var parsedId))
                            {
                                relatedQuestionIds.Add(parsedId);
                            }
                            // TODO: If question_id is UUID, you may need to add a mapping table
                            // or store post_id directly in Qdrant payload
                        }
                    }
                }
            }
            else
            {
                contextBuilder.AppendLine("Không tìm thấy câu trả lời liên quan trong cơ sở dữ liệu.");
            }

            var context = contextBuilder.ToString();

            // Step 6: Call AI model to generate answer
            string aiAnswer;
            try
            {
                aiAnswer = await _aiService.GenerateAnswerAsync(question, context, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling AI service: {ex.Message}");
                // Fallback answer if AI service fails
                aiAnswer = "Xin lỗi, tôi không thể tạo câu trả lời vào lúc này. Vui lòng thử lại sau.";
            }

            // Step 7: Return result
            return new AskQuestionWithAIResponse
            {
                Answer = aiAnswer,
                RelatedQuestionIds = relatedQuestionIds.ToList(),
                ContextItemsUsed = filteredResults.Count
            };
        }
    }
}

