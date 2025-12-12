namespace Application.Posts.DTOs
{
    public class AskQuestionWithAIRequest
    {
        public string Question { get; set; } = default!;
        public double? SimilarityThreshold { get; set; } // Optional threshold for filtering results
        public int? MaxContextItems { get; set; } // Max number of context items to use
    }
}

