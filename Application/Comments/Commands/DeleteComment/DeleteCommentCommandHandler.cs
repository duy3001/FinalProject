using Application.Comments.Commands.DeleteComment;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Comments.Commands.DeleteComment
{
    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _current;
        private readonly IVectorSearchService _vectorSearch;

        public DeleteCommentCommandHandler(
            IApplicationDbContext db, 
            ICurrentUserService current,
            IVectorSearchService vectorSearch)
        {
            _db = db;
            _current = current;
            _vectorSearch = vectorSearch;
        }

        public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken ct)
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

            // Soft delete: set IsDeleted flag
            cmt.IsDeleted = true;
            cmt.DeletedAt = DateTime.UtcNow;
            cmt.DeletedByMemberId = me;

            await _db.SaveChangesAsync(ct);

            // Delete from Qdrant if this is a top-level comment (not a reply)
            if (cmt.ParentCommentId == null)
            {
                try
                {
                    await DeleteAnswerFromQdrantAsync(cmt.Id, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting answer from Qdrant: {ex.Message}");
                    // Continue even if Qdrant delete fails
                }
            }

            return true;
        }

        private async Task DeleteAnswerFromQdrantAsync(long commentId, CancellationToken ct)
        {
            var answerId = $"answer-{commentId}";
            var collectionName = "answers";

            // Check if collection exists before trying to delete
            var exists = await _vectorSearch.CollectionExistsAsync(collectionName, ct);
            if (exists)
            {
                await _vectorSearch.DeletePointAsync(collectionName, answerId, ct);
            }
        }
    }
}

