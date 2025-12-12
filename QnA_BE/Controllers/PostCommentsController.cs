using Application.Comments.Commands.AddComment;
using Application.Comments.Commands.UpdateComment;
using Application.Comments.Commands.VoteComment;
using Application.Comments.Commands.DeleteComment;
using Application.Comments.DTOs;
using Application.Comments.Queries.GetCommentRevisionDetail;
using Application.Comments.Queries.GetCommentRevisions;
using Application.Comments.Queries.GetCommentsByPost;
using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Comments.Commands.UploadCommentImage;
using Application.Comments.Commands.AddCommentLink;
using Application.Comments.Commands.RemoveCommentAttachment;
using Application.Comments.Queries.GetCommentAttachments;

namespace QnA_BE.Controllers
{
    [ApiController]
    [Route("api/v1/posts/{postId:long}/comments")]
    public class PostCommentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PostCommentsController(IMediator mediator) => _mediator = mediator;

        // GET /api/v1/posts/{postId}/comments?page=
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Paged<CommentDto>>> List(long postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var res = await _mediator.Send(new GetCommentsByPostQuery(postId, page, pageSize), ct);
            return Ok(res);
        }

        // POST /api/v1/posts/{postId}/comments (auth)
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<CommentDto>> Create(long postId, [FromBody] CreateCommentRequest request, CancellationToken ct)
        {
            var dto = await _mediator.Send(new AddCommentCommand(postId, request), ct);
            return CreatedAtAction(nameof(List), new { postId, page = 1, pageSize = 20 }, dto);
        }

        // PUT /api/v1/posts/{postId}/comments/{commentId} (auth)
        [HttpPut("{commentId:long}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CommentDto>> Update(long postId, long commentId, [FromBody] UpdateCommentRequest request, CancellationToken ct)
        {
            // postId ở route là để đảm bảo URL đẹp/đúng ngữ cảnh; xử lý dựa vào commentId
            var dto = await _mediator.Send(new UpdateCommentCommand(commentId, request), ct);
            return Ok(dto);
        }

        // DELETE /api/v1/posts/{postId}/comments/{commentId} (auth)
        [HttpDelete("{commentId:long}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> Delete(long postId, long commentId, CancellationToken ct)
        {
            var ok = await _mediator.Send(new DeleteCommentCommand(commentId), ct);
            return Ok(new { success = ok });
        }

        // POST /api/v1/posts/{postId}/comments/{commentId}/vote (auth)
        [HttpPost("{commentId:long}/vote")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> Vote(long postId, long commentId, [FromBody] Application.Posts.DTOs.VoteRequest request, CancellationToken ct)
        {
            var res = await _mediator.Send(new VoteCommentCommand(commentId, request.Value), ct);
            return Ok(new { score = res.Score, myVote = res.MyVote });
        }

        // GET /api/v1/posts/{postId}/comments/{commentId}/revisions?page=
        [HttpGet("{commentId:long}/revisions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Paged<CommentRevisionDto>>> GetRevisions(long postId, long commentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var res = await _mediator.Send(new GetCommentRevisionsQuery(commentId, page, pageSize), ct);
            return Ok(res);
        }

        // GET /api/v1/posts/{postId}/comments/{commentId}/revisions/{revId}
        [HttpGet("{commentId:long}/revisions/{revId:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CommentRevisionDetailDto>> GetRevisionDetail(long postId, long commentId, long revId, CancellationToken ct = default)
        {
            var dto = await _mediator.Send(new GetCommentRevisionDetailQuery(commentId, revId), ct);
            return Ok(dto);
        }
        // GET /api/v1/posts/{postId}/comments/{commentId}/attachments
        [HttpGet("{commentId:long}/attachments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Application.Comments.DTOs.CommentAttachmentDto>>> GetCommentAttachments(
            long postId, long commentId, CancellationToken ct = default)
        {
            var items = await _mediator.Send(new GetCommentAttachmentsQuery(commentId), ct);
            return Ok(items);
        }

        // POST /api/v1/posts/{postId}/comments/{commentId}/attachments/images (multipart)
        [HttpPost("{commentId:long}/attachments/images")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Application.Comments.DTOs.CommentAttachmentDto>> UploadCommentImage(
            long postId, long commentId, IFormFile file, [FromForm] string? caption, CancellationToken ct = default)
        {
            var dto = await _mediator.Send(new UploadCommentImageCommand(commentId, file, caption), ct);
            return Ok(dto);
        }

        // POST /api/v1/posts/{postId}/comments/{commentId}/attachments/links
        [HttpPost("{commentId:long}/attachments/links")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Application.Comments.DTOs.CommentAttachmentDto>> AddCommentLink(
            long postId, long commentId, [FromBody] AddCommentLinkRequest req, CancellationToken ct = default)
        {
            var dto = await _mediator.Send(new AddCommentLinkCommand(commentId, req), ct);
            return Ok(dto);
        }

        // DELETE /api/v1/posts/{postId}/comments/{commentId}/attachments/{attId}
        [HttpDelete("{commentId:long}/attachments/{attId:long}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> RemoveCommentAttachment(
            long postId, long commentId, long attId, CancellationToken ct = default)
        {
            var ok = await _mediator.Send(new RemoveCommentAttachmentCommand(commentId, attId), ct);
            return Ok(new { success = ok });
        }

    }
}
