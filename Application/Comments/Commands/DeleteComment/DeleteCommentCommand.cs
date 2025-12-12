using MediatR;

namespace Application.Comments.Commands.DeleteComment
{
    public record DeleteCommentCommand(long CommentId) : IRequest<bool>;
}

