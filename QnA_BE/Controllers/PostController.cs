using Application.Common.Models;
using Application.Posts.Commands.AskQuestionWithAI;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Commands.UpdatePost;
using Application.Posts.Commands.VotePost;
using Application.Posts.DTOs;
using Application.Posts.Queries.GetPostDetail;
using Application.Posts.Queries.GetPostRevisionDetail;
using Application.Posts.Queries.GetPostRevisions;
using Application.Posts.Queries.ListByTag;
using Application.Posts.Queries.ListPosts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Posts.Commands.UploadPostImage;
using Application.Posts.Commands.AddPostLink;
using Application.Posts.Commands.RemoveAttachment;
using Application.Posts.Queries.GetPostAttachments;
using Microsoft.AspNetCore.Http;

namespace QnA_BE.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class PostController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PostController(IMediator mediator) => _mediator = mediator;

        // GET /api/v1/posts?page=&pageSize=&q=&sort=
        [HttpGet("posts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Paged<PostDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
                                                             [FromQuery] string? q = null, [FromQuery] string? sort = "created_desc",
                                                             CancellationToken ct = default)
        {
            var res = await _mediator.Send(new ListPostsQuery(page, pageSize, q, sort), ct);
            return Ok(res);
        }

        // GET /api/v1/posts/{id}
        [HttpGet("posts/{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PostDetailDto>> GetDetail([FromRoute] long id, CancellationToken ct)
        {
            var dto = await _mediator.Send(new GetPostDetailQuery(id), ct);
            return Ok(dto);
        }

        // POST /api/v1/posts (auth)
        [HttpPost("posts")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<PostDetailDto>> Create([FromBody] CreatePostRequest request, CancellationToken ct)
        {
            var dto = await _mediator.Send(new CreatePostCommand(request), ct);
            return CreatedAtAction(nameof(GetDetail), new { id = dto.Id }, dto);
        }

        // PUT /api/v1/posts/{id} (auth, owner/mod)
        [HttpPut("posts/{id:long}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PostDetailDto>> Update([FromRoute] long id, [FromBody] UpdatePostRequest request, CancellationToken ct)
        {
            var dto = await _mediator.Send(new UpdatePostCommand(id, request), ct);
            return Ok(dto);
        }

        // POST /api/v1/posts/{id}/vote (auth)
        [HttpPost("posts/{id:long}/vote")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> Vote([FromRoute] long id, [FromBody] VoteRequest request, CancellationToken ct)
        {
            var (score, myVote) = await _mediator.Send(new VotePostCommand(id, request.Value), ct);
            return Ok(new { score, myVote });
        }

        // GET /api/v1/tags/{tag}/posts?page=
        [HttpGet("tags/{tag}/posts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Paged<PostDto>>> ListByTag([FromRoute] string tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var res = await _mediator.Send(new ListPostsByTagQuery(tag, page, pageSize), ct);
            return Ok(res);
        }

        // GET /api/v1/posts/{id}/revisions?page=&pageSize=
        [HttpGet("posts/{id:long}/revisions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Paged<RevisionDto>>> GetRevisions(
            [FromRoute] long id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var res = await _mediator.Send(new GetPostRevisionsQuery(id, page, pageSize), ct);
            return Ok(res);
        }

        // GET /api/v1/posts/{id}/revisions/{revId}
        [HttpGet("posts/{id:long}/revisions/{revId:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<RevisionDetailDto>> GetRevisionDetail(
            [FromRoute] long id,
            [FromRoute] long revId,
            CancellationToken ct = default)
        {
            var dto = await _mediator.Send(new GetPostRevisionDetailQuery(id, revId), ct);
            return Ok(dto);
        }

        // GET /api/v1/posts/{id}/attachments
        [HttpGet("posts/{id:long}/attachments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AttachmentDto>>> GetAttachments([FromRoute] long id, CancellationToken ct = default)
        {
            var items = await _mediator.Send(new GetPostAttachmentsQuery(id), ct);
            return Ok(items);
        }

        // POST /api/v1/posts/{id}/attachments/images (multipart)
        [HttpPost("posts/{id:long}/attachments/images")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AttachmentDto>> UploadImage([FromRoute] long id, IFormFile file, [FromForm] string? caption, CancellationToken ct = default)
        {
            var dto = await _mediator.Send(new UploadPostImageCommand(id, file, caption), ct);
            return Ok(dto);
        }

        // POST /api/v1/posts/{id}/attachments/links
        [HttpPost("posts/{id:long}/attachments/links")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AttachmentDto>> AddLink([FromRoute] long id, [FromBody] AddLinkRequest req, CancellationToken ct = default)
        {
            var dto = await _mediator.Send(new AddPostLinkCommand(id, req), ct);
            return Ok(dto);
        }

        // DELETE /api/v1/posts/{id}/attachments/{attId}
        [HttpDelete("posts/{id:long}/attachments/{attId:long}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> RemoveAttachment([FromRoute] long id, [FromRoute] long attId, CancellationToken ct = default)
        {
            var ok = await _mediator.Send(new RemoveAttachmentCommand(id, attId), ct);
            return Ok(new { success = ok });
        }

        // POST /api/v1/posts/ask-ai
        [HttpPost("posts/ask-ai")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AskQuestionWithAIResponse>> AskQuestionWithAI([FromBody] AskQuestionWithAIRequest request, CancellationToken ct = default)
        {
            var response = await _mediator.Send(new AskQuestionWithAICommand(request), ct);
            return Ok(response);
        }

    }
}
