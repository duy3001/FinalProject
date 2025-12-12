using Application.Common.Interfaces;
using Application.Posts.DTOs;
using Application.Posts.Queries.GetPostDetail;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Posts.Handlers
{
    public class GetPostDetailQueryHandler : IRequestHandler<GetPostDetailQuery, PostDetailDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _current;

        public GetPostDetailQueryHandler(IApplicationDbContext db, ICurrentUserService current)
        { _db = db; _current = current; }

        public async Task<PostDetailDto> Handle(GetPostDetailQuery request, CancellationToken ct)
        {
            var post = await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

            if (post == null) throw new KeyNotFoundException("Post not found.");

            var score = await _db.PostVotes.Where(v => v.PostId == post.Id).SumAsync(v => v.Value, ct);

            int? myVote = null;
            var me = _current.CurrentMemberId;
            if (me.HasValue)
            {
                var v = await _db.PostVotes.FirstOrDefaultAsync(x => x.PostId == post.Id && x.MemberId == me.Value, ct);
                myVote = v?.Value;
            }

            // Get related posts (same tags, exclude current post)
            var tagIds = post.PostTags.Select(pt => pt.TagId).ToList();
            List<PostDto> relatedPosts = new();
            
            if (tagIds.Any())
            {
                var relatedPostsQuery = _db.Posts
                    .Include(p => p.Author)
                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                    .Include(p => p.Category)
                    .Where(p => p.Id != post.Id && p.PostTags.Any(pt => tagIds.Contains(pt.TagId)))
                    .Distinct();

                var relatedScores = _db.PostVotes
                    .GroupBy(v => v.PostId)
                    .Select(g => new { PostId = g.Key, Score = g.Sum(x => x.Value) });

                var relatedJoined = from p in relatedPostsQuery
                                    join s in relatedScores on p.Id equals s.PostId into ps
                                    from s in ps.DefaultIfEmpty()
                                    select new { p, Score = (int?)s.Score ?? 0 };

                relatedPosts = await relatedJoined
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.p.CreatedAt)
                    .Take(5)
                    .Select(x => new PostDto
                    {
                        Id = x.p.Id,
                        Title = x.p.Title,
                        AuthorDisplayName = x.p.Author!.DisplayName,
                        CreatedAt = x.p.CreatedAt,
                        Score = x.Score,
                        Tags = x.p.PostTags.Select(pt => pt.Tag!.Name).ToList(),
                        PreviewBody = x.p.Body.Length > 160 ? x.p.Body.Substring(0, 160) + "..." : x.p.Body,
                        CategoryId = x.p.CategoryId,
                        CategorySlug = x.p.Category != null ? x.p.Category.Slug : null,
                        CategoryName = x.p.Category != null ? x.p.Category.Name : null
                    })
                    .ToListAsync(ct);
            }

            return new PostDetailDto
            {
                Id = post.Id,
                Title = post.Title,
                Body = post.Body,
                CreatedAt = post.CreatedAt,
                AuthorDisplayName = post.Author?.DisplayName ?? "unknown",
                Score = score,
                MyVote = myVote,
                Tags = post.PostTags.Select(pt => pt.Tag!.Name).ToList(),
                CategoryId = post.CategoryId,
                CategorySlug = post.Category?.Slug,
                CategoryName = post.Category?.Name,
                RelatedPosts = relatedPosts
            };
        }
    }
}
