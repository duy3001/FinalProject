using Application.Common.Interfaces;
using Application.Common.Utils;
using Application.Posts.Commands.CreatePost;
using Application.Posts.DTOs;
using Domain.Entities;
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
    public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostDetailDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _current;
        private readonly ITenantProvider _tenant;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorSearchService _vectorSearch;
        private readonly IAIService _aiService;
        private readonly double _defaultThreshold = 0.7;
        private readonly int _defaultMaxContext = 5;

        public CreatePostCommandHandler(
            IApplicationDbContext db, 
            ICurrentUserService current, 
            ITenantProvider tenant,
            IEmbeddingService embeddingService,
            IVectorSearchService vectorSearch,
            IAIService aiService)
        {
            _db = db; 
            _current = current; 
            _tenant = tenant;
            _embeddingService = embeddingService;
            _vectorSearch = vectorSearch;
            _aiService = aiService;
        }

        public async Task<PostDetailDto> Handle(CreatePostCommand request, CancellationToken ct)
        {
            var dto = request.Request;
            var authorId = _current.CurrentMemberId ?? throw new UnauthorizedAccessException("Login required.");

            Category? category = null;

            if (!string.IsNullOrWhiteSpace(dto.CategorySlug))
            {
                category = await _db.Categories.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Slug == dto.CategorySlug, ct)
                    ?? throw new ArgumentException("CategorySlug không tồn tại.");

                if (category.IsHidden)
                    throw new UnauthorizedAccessException("Không thể đăng vào category ẩn.");
            }
            else if (dto.CategoryId.HasValue)
            {
                category = await _db.Categories.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value, ct)
                    ?? throw new ArgumentException("CategoryId không tồn tại.");

                if (category.IsHidden)
                    throw new UnauthorizedAccessException("Không thể đăng vào category ẩn.");
            }


            var post = new Post
            {
                Title = dto.Title.Trim(),
                Body = dto.Body,
                AuthorId = authorId,
                CategoryId = category?.Id
            };
            _db.Posts.Add(post);
            await _db.SaveChangesAsync(ct);

            // Tags
            var tags = new List<Tag>();
            if (dto.Tags != null && dto.Tags.Count > 0)
            {
                var tenantId = _tenant.GetTenantId();
                var slugs = dto.Tags.Select(SlugGenerator.Slugify)
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Distinct()
                                    .ToList();

                var existing = await _db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync(ct);
                var missing = slugs.Except(existing.Select(e => e.Slug)).ToList();

                foreach (var ms in missing)
                {
                    var name = dto.Tags.First(t => SlugGenerator.Slugify(t) == ms);
                    var tag = new Tag { Name = name, Slug = ms, TenantId = tenantId };
                    _db.Tags.Add(tag);
                    existing.Add(tag);
                }
                await _db.SaveChangesAsync(ct);

                tags = existing;
                foreach (var tag in tags)
                    _db.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });

                await _db.SaveChangesAsync(ct);
            }
            var catName = category?.Name;
            var catSlug = category?.Slug;
            var catId = category?.Id;

            // Generate AI answer automatically
            string? aiAnswer = null;
            List<long> relatedQuestionIds = new();

            try
            {
                var questionText = $"{post.Title}\n\n{post.Body}";
                
                // Step 2: Embed the question
                var questionVector = await _embeddingService.GenerateEmbeddingAsync(questionText, ct);

                // Step 3: Search Qdrant
                var searchResults = await _vectorSearch.SearchAsync(
                    collectionName: "answers",
                    queryVector: questionVector,
                    limit: _defaultMaxContext * 2,
                    filter: new Dictionary<string, object> { { "is_active", true } },
                    ct: ct
                );

                // Step 4: Process search results (apply threshold)
                var filteredResults = searchResults
                    .Where(r => r.Score >= _defaultThreshold)
                    .OrderByDescending(r => r.Score)
                    .Take(_defaultMaxContext)
                    .ToList();

                // Step 5: Build context for AI
                var contextBuilder = new StringBuilder();
                var relatedIds = new HashSet<long>();

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

                        // Extract question_id from payload
                        if (result.Payload.TryGetValue("post_id", out var postIdObj))
                        {
                            if (postIdObj is long pId)
                            {
                                relatedIds.Add(pId);
                            }
                            else if (postIdObj is string postIdStr && long.TryParse(postIdStr, out var parsedId))
                            {
                                relatedIds.Add(parsedId);
                            }
                        }
                        else if (result.Payload.TryGetValue("question_id", out var questionIdObj))
                        {
                            if (questionIdObj is long qId)
                            {
                                relatedIds.Add(qId);
                            }
                            else if (questionIdObj is string questionIdStr && long.TryParse(questionIdStr, out var parsedId))
                            {
                                relatedIds.Add(parsedId);
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
                aiAnswer = await _aiService.GenerateAnswerAsync(questionText, context, ct);
                relatedQuestionIds = relatedIds.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating AI answer: {ex.Message}");
                // Continue without AI answer if it fails
            }

            return new PostDetailDto
            {
                Id = post.Id,
                Title = post.Title,
                Body = post.Body,
                CreatedAt = post.CreatedAt,
                AuthorDisplayName = (await _db.Members.FindAsync(new object[] { authorId }, ct))?.DisplayName ?? "unknown",
                Score = 0,
                MyVote = null,
                Tags = tags.Select(t => t.Name).ToList(),
                CategoryId = catId,
                CategorySlug = catSlug,
                CategoryName = catName,
                AIAnswer = aiAnswer,
                RelatedQuestionIds = relatedQuestionIds
            };
        }
    }
}
