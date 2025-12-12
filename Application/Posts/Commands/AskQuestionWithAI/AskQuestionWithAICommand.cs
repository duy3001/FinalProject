using Application.Posts.DTOs;
using MediatR;

namespace Application.Posts.Commands.AskQuestionWithAI
{
    public record AskQuestionWithAICommand(AskQuestionWithAIRequest Request) : IRequest<AskQuestionWithAIResponse>;
}

