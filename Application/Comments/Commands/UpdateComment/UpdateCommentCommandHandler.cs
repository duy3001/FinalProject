using Application.Comments.Commands.UpdateComment;
using Application.Comments.DTOs;
using Application.Common.Interfaces;
using Application.Posts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Comments.Commands.UpdateComment
{
    public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _current;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorSearchService _vectorSearch;

        public UpdateCommentCommandHandler(
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

        public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken ct)
        {
            var me = _current.CurrentMemberId ?? throw new UnauthorizedAccessException("Login required.");

            var cmt = await _db.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
                ?? throw new KeyNotFoundException("Comment not found.");

            var myMember = await _db.Members.FindAsync(new object[] { me }, ct);
            var isOwner = cmt.AuthorId == me;
            var isMod = (myMember?.IsModerator == true) || (myMember?.IsAdministrator == true);
            if (!isOwner && !isMod) throw new UnauthorizedAccessException("Forbidden.");

            var dto = request.Request;
            var beforeBody = cmt.Body;

            if (!string.IsNullOrWhiteSpace(dto.Body))
                cmt.Body = dto.Body;

            // Lưu revision
            _db.CommentRevisions.Add(new CommentRevision
            {
                CommentId = cmt.Id,
                BeforeBody = beforeBody,
                AfterBody = cmt.Body,
                EditorId = me,
                Summary = dto.Summary ?? "Cập nhật bình luận"
            });

            await _db.SaveChangesAsync(ct);

            // Update Qdrant if this is a top-level comment (not a reply)
            if (cmt.ParentCommentId == null)
            {
                try
                {
                    await UpdateAnswerInQdrantAsync(cmt.Id, cmt.PostId, cmt.Body, cmt.CreatedAt, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating answer in Qdrant: {ex.Message}");
                    // Continue even if Qdrant update fails
                }
            }

            // tính score
            var score = await _db.CommentVotes.Where(v => v.CommentId == cmt.Id).SumAsync(v => v.Value, ct);

            return new CommentDto
            {
                Id = cmt.Id,
                PostId = cmt.PostId,
                Body = cmt.Body,
                AuthorDisplayName = cmt.Author?.DisplayName ?? "unknown",
                CreatedAt = cmt.CreatedAt
                // Nếu bạn có thêm Score/MyVote vào CommentDto thì set ở đây
            };
        }

        private async Task UpdateAnswerInQdrantAsync(long commentId, long postId, string answerText, DateTime createdAt, CancellationToken ct)
        {
            // Generate embedding for the updated answer
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
                { "post_id", postId },
                { "comment_id", commentId }
            };

            // Ensure collection exists
            var collectionName = "answers";
            var exists = await _vectorSearch.CollectionExistsAsync(collectionName, ct);
            if (!exists)
            {
                var vectorSize = _embeddingService.GetVectorSize();
                await _vectorSearch.CreateCollectionAsync(collectionName, vectorSize, ct);
            }

            // Upsert to Qdrant (will update if exists, create if not)
            await _vectorSearch.UpsertPointAsync(collectionName, answerId, vector, payload, ct);
        }
    }
}
