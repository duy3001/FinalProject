using System;
using System.Collections.Generic;

namespace Application.Posts.DTOs
{
    public class PostDetailDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = default!;
        public string Body { get; set; } = default!;
        public string AuthorDisplayName { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public int Score { get; set; }
        public int? MyVote { get; set; } // null nếu chưa đăng nhập
        public List<string> Tags { get; set; } = new();
        public long? CategoryId { get; init; }
        public string? CategorySlug { get; init; }
        public string? CategoryName { get; init; }
        public List<PostDto> RelatedPosts { get; set; } = new();
        public string? AIAnswer { get; set; } // AI-generated answer
        public List<long> RelatedQuestionIds { get; set; } = new(); // Related question IDs from AI search
    }
}
