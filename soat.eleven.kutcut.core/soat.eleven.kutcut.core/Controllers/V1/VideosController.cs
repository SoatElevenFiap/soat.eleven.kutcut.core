using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Common;
using soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Video;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.core.api.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly IVideoService _videoService;

        public VideosController(IVideoService videoService)
        {
            _videoService = videoService;
        }

        /// <summary>
        /// Upload de um novo vídeo com metadados.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromForm] CreateVideoRequest request, IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            var input = new CreateVideoInput(
                request.Title,
                file.FileName,
                stream);

            var result = await _videoService.CreateAsync(input);

            if (result.IsFailed)
                return BadRequest(new ErrorResponse(
                    "Erro de validação",
                    StatusCodes.Status400BadRequest,
                    result.Errors.Select(e => e.Message)));

            var video = result.Value;
            return CreatedAtAction(nameof(GetById), new { id = video.Id }, MapToResponse(video));
        }

        /// <summary>
        /// Listagem paginada de vídeos.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VideoResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
        {
            var result = await _videoService.GetAllAsync(query.PageNumber, query.PageSize);

            var paged = result.Value;
            return Ok(new PagedResponse<VideoResponse>(
                paged.Items.Select(MapToResponse),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize,
                paged.TotalPages));
        }

        /// <summary>
        /// Detalhes de um vídeo por ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _videoService.GetByIdAsync(id);

            if (result.IsFailed)
                return NotFound(new ErrorResponse(
                    "Não encontrado",
                    StatusCodes.Status404NotFound,
                    result.Errors.Select(e => e.Message)));

            return Ok(MapToResponse(result.Value));
        }

        /// <summary>
        /// Filtro de vídeos por status com paginação.
        /// </summary>
        [HttpGet("status/{status}")]
        [ProducesResponseType(typeof(PagedResponse<VideoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByStatus(StatusEnum status, [FromQuery] PaginationQuery query)
        {
            if (!Enum.IsDefined(typeof(StatusEnum), status))
                return BadRequest(new ErrorResponse(
                    "Status inválido",
                    StatusCodes.Status400BadRequest,
                    new[] { "O status informado não é válido." }));

            var result = await _videoService.GetByStatusAsync(status, query.PageNumber, query.PageSize);

            var paged = result.Value;
            return Ok(new PagedResponse<VideoResponse>(
                paged.Items.Select(MapToResponse),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize,
                paged.TotalPages));
        }

        /// <summary>
        /// Atualização parcial do título do vídeo.
        /// </summary>
        [HttpPatch("{id:guid}")]
        [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateVideoTitleRequest request)
        {
            var result = await _videoService.UpdateTitleAsync(id, request.Title);

            if (result.IsFailed)
            {
                var isNotFound = result.Errors.Any(e => e.Message.Contains("não encontrado"));
                var statusCode = isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                var title = isNotFound ? "Não encontrado" : "Erro de validação";

                return StatusCode(statusCode, new ErrorResponse(
                    title,
                    statusCode,
                    result.Errors.Select(e => e.Message)));
            }

            return Ok(MapToResponse(result.Value));
        }

        /// <summary>
        /// Download do arquivo .zip de thumbnails.
        /// </summary>
        [HttpGet("{id:guid}/thumbnails/download")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(Guid id)
        {
            var result = await _videoService.DownloadThumbnailsAsync(id);

            if (result.IsFailed)
                return NotFound(new ErrorResponse(
                    "Não encontrado",
                    StatusCodes.Status404NotFound,
                    result.Errors.Select(e => e.Message)));

            return File(result.Value, "application/zip", $"{id}.zip");
        }

        /// <summary>
        /// Exclusão de um vídeo por ID.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _videoService.DeleteAsync(id);

            if (result.IsFailed)
                return NotFound(new ErrorResponse(
                    "Não encontrado",
                    StatusCodes.Status404NotFound,
                    result.Errors.Select(e => e.Message)));

            return NoContent();
        }

        private static VideoResponse MapToResponse(VideoResult video) =>
            new(video.Id,
                video.Title,
                video.UserId,
                video.Filename,
                video.Status.ToString(),
                video.StatusDescription,
                video.CreatedAt,
                video.UpdatedAt);
    }
}
