using Application.Comments.Commands.AddComment;
using Application.Comments.DTOs;
using Application.Common.Interfaces;
using Domain.Common.Enums;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Comments.Handlers
{
    public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _current;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorSearchService _vectorSearch;

        public AddCommentCommandHandler(
            IApplicationDbContext db, 
            ICurrentUserService current,
            IEmbeddingService embeddingService,
            IVectorSearchService vectorSearch)
        {
            _db = db; 
            _current = current;
            _embeddingService = embeddingService;
            _vectorSearch = vectorSearch;
        }

        public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken ct)
        {
            var me = _current.CurrentMemberId ?? throw new UnauthorizedAccessException("Login required.");
            var body = (request.Request.Body ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Comment body is required.");

            // Lấy thông tin Post (cần AuthorId để notify)
            var post = await _db.Posts
                .AsNoTracking()
                .Where(p => p.Id == request.PostId)
                .Select(p => new { p.Id, p.AuthorId, p.Title })
                .FirstOrDefaultAsync(ct);
            if (post == null) throw new KeyNotFoundException("Post not found.");

            // Nếu là reply, lấy parent comment (cần AuthorId để notify)
            long? parentId = request.Request.ParentCommentId;
            var parentComment = parentId.HasValue
                ? await _db.Comments.AsNoTracking()
                    .Where(c => c.Id == parentId.Value)
                    .Select(c => new { c.Id, c.AuthorId, c.PostId })
                    .FirstOrDefaultAsync(ct)
                : null;

            var cmt = new Comment
            {
                PostId = request.PostId,
                AuthorId = me,
                Body = body,
                ParentCommentId = parentId
            };
            _db.Comments.Add(cmt);
            await _db.SaveChangesAsync(ct); // cần để có cmt.Id

            // Tạo notification (đơn luồng, tuần tự)
            var actorId = me;
            var excerpt = body.Length > 140 ? body.Substring(0, 140) + "..." : body;

            if (parentComment == null)
            {
                var recipientId = post.AuthorId;
                if (recipientId != actorId)
                {
                    _db.Notifications.Add(new Notification
                    {
                        RecipientId = recipientId,
                        ActorId = actorId,
                        Type = NotificationType.PostCommented,
                        PostId = post.Id,
                        CommentId = cmt.Id,
                        DataJson = excerpt
                    });
                    await _db.SaveChangesAsync(ct);
                }
            }
            else
            {
                var recipientId = parentComment.AuthorId;
                if (recipientId != actorId)
                {
                    _db.Notifications.Add(new Notification
                    {
                        RecipientId = recipientId,
                        ActorId = actorId,
                        Type = NotificationType.CommentReplied,
                        PostId = post.Id,
                        CommentId = cmt.Id,
                        DataJson = excerpt
                    });
                    await _db.SaveChangesAsync(ct);
                }
            }

            // lấy display name
            var authorName = await _db.Members
                .AsNoTracking()
                .Where(m => m.Id == me)
                .Select(m => m.DisplayName)
                .FirstOrDefaultAsync(ct) ?? "unknown";

            // Save to Qdrant (only for top-level comments, not replies)
            if (parentComment == null)
            {
                try
                {
                    await SaveAnswerToQdrantAsync(cmt.Id, post.Id, body, cmt.CreatedAt, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving answer to Qdrant: {ex.Message}");
                    // Continue even if Qdrant save fails
                }
            }

            return new CommentDto
            {
                Id = cmt.Id,
                PostId = cmt.PostId,
                Body = cmt.Body,
                AuthorDisplayName = authorName,
                CreatedAt = cmt.CreatedAt
            };
        }

        private async Task SaveAnswerToQdrantAsync(long commentId, long postId, string answerText, DateTime createdAt, CancellationToken ct)
        {
            // Generate embedding for the answer
            var vector = await _embeddingService.GenerateEmbeddingAsync(answerText, ct);

            // Generate IDs (using format: answer-{commentId} and question-{postId})
            var answerId = $"answer-{commentId}";
            var questionId = $"question-{postId}";

            // Build payload according to the required format
            var payload = new Dictionary<string, object>
            {
                { "answer_id", answerId },
                { "question_id", questionId },
                { "answer_text", answerText },
                { "is_active", true },
                { "created_at", createdAt.ToString("O") },
                { "post_id", postId }, // Also store post_id for easier lookup
                { "comment_id", commentId } // Also store comment_id for easier lookup
            };

            // Ensure collection exists
            var collectionName = "answers";
            var exists = await _vectorSearch.CollectionExistsAsync(collectionName, ct);
            if (!exists)
            {
                var vectorSize = _embeddingService.GetVectorSize();
                await _vectorSearch.CreateCollectionAsync(collectionName, vectorSize, ct);
            }

            // Upsert to Qdrant
            await _vectorSearch.UpsertPointAsync(collectionName, answerId, vector, payload, ct);
        }
    }
}
