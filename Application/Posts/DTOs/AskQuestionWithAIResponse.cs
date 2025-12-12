using System.Collections.Generic;

namespace Application.Posts.DTOs
{
    public class AskQuestionWithAIResponse
    {
        public string Answer { get; set; } = default!;
        public List<long> RelatedQuestionIds { get; set; } = new();
        public int ContextItemsUsed { get; set; }
    }
}

