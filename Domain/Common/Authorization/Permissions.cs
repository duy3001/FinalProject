namespace Domain.Common.Authorization
{
    public static class Permissions
    {
        public static class QnA
        {
            public const string AskQuestion = "qna.ask";
            public const string Answer = "qna.answer";
            public const string Comment = "qna.comment";
            public const string Vote = "qna.vote";
            public const string Search = "qna.search";
            public const string Delete = "qna.delete";
        }

        public static class Admin
        {
            public const string ViewStats = "admin.view_stats";
            public const string UploadPolicyDocs = "admin.upload_policy_docs";
            public const string ManagePolicyDataset = "admin.manage_policy_dataset";
        }
    }
}
