using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Files.Commands;
using CommonHall.Application.Features.Files.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Security.Cryptography;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public FilesController(
        IMediator mediator,
        IApplicationDbContext context,
        IFileStorageService fileStorage)
    {
        _mediator = mediator;
        _context = context;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Upload a single file
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(52_428_800)] // 50MB
    [ProducesResponseType(typeof(ApiResponse<StoredFileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
        IFormFile file,
        [FromQuery] Guid? collectionId = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<StoredFileDto>.Failure("INVALID_FILE", "No file provided."));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadFileCommand
        {
            Stream = stream,
            OriginalName = file.FileName,
            MimeType = file.ContentType,
            CollectionId = collectionId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Created($"/api/v1/files/{result.Id}", ApiResponse<StoredFileDto>.Success(result));
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("batch")]
    [RequestSizeLimit(262_144_000)] // 250MB total for batch
    [ProducesResponseType(typeof(ApiResponse<List<StoredFileDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleFiles(
        List<IFormFile> files,
        [FromQuery] Guid? collectionId = null,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(ApiResponse<List<StoredFileDto>>.Failure("INVALID_FILES", "No files provided."));
        }

        var uploadItems = new List<FileUploadItem>();
        var streams = new List<Stream>();

        try
        {
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                uploadItems.Add(new FileUploadItem
                {
                    Stream = stream,
                    OriginalName = file.FileName,
                    MimeType = file.ContentType
                });
            }

            var command = new UploadMultipleFilesCommand
            {
                Files = uploadItems,
                CollectionId = collectionId
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Created("/api/v1/files", ApiResponse<List<StoredFileDto>>.Success(result));
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<StoredFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetFileQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<StoredFileDto>.Success(result));
    }

    /// <summary>
    /// Download a file with streaming, Range support, and caching headers
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        Guid id,
        [FromQuery] bool inline = false,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        var stream = await _fileStorage.GetStreamAsync(file.StoragePath, cancellationToken);
        if (stream is null)
        {
            return NotFound();
        }

        // Generate ETag
        var etag = GenerateETag(file.Id, file.UpdatedAt);

        // Check If-None-Match
        if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) &&
            ifNoneMatch.ToString().Trim('"') == etag)
        {
            stream.Dispose();
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // Set caching headers
        Response.Headers[HeaderNames.ETag] = $"\"{etag}\"";
        Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000"; // 1 year
        Response.Headers[HeaderNames.LastModified] = file.UpdatedAt.ToString("R");

        // Content-Disposition
        var disposition = inline ? "inline" : "attachment";
        Response.Headers[HeaderNames.ContentDisposition] = $"{disposition}; filename=\"{file.OriginalName}\"";

        // Handle Range requests
        if (Request.Headers.TryGetValue(HeaderNames.Range, out var rangeHeader))
        {
            var rangeHeaderValue = RangeHeaderValue.Parse(rangeHeader.ToString());
            var range = rangeHeaderValue.Ranges.FirstOrDefault();

            if (range != null && stream.CanSeek)
            {
                var from = range.From ?? 0;
                var to = range.To ?? file.SizeBytes - 1;
                var length = to - from + 1;

                stream.Seek(from, SeekOrigin.Begin);

                Response.StatusCode = StatusCodes.Status206PartialContent;
                Response.Headers[HeaderNames.ContentRange] = $"bytes {from}-{to}/{file.SizeBytes}";
                Response.Headers[HeaderNames.ContentLength] = length.ToString();

                return File(stream, file.MimeType, enableRangeProcessing: false);
            }
        }

        return File(stream, file.MimeType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Get file thumbnail
    /// </summary>
    [HttpGet("{id:guid}/thumbnail")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(Guid id, CancellationToken cancellationToken)
    {
        var file = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (file is null || string.IsNullOrEmpty(file.ThumbnailPath))
        {
            return NotFound();
        }

        var stream = await _fileStorage.GetStreamAsync(file.ThumbnailPath, cancellationToken);
        if (stream is null)
        {
            return NotFound();
        }

        // Set caching headers
        var etag = GenerateETag(file.Id, file.UpdatedAt) + "-thumb";
        Response.Headers[HeaderNames.ETag] = $"\"{etag}\"";
        Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000";

        return File(stream, file.MimeType);
    }

    /// <summary>
    /// Update file metadata (alt text)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StoredFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFile(Guid id, [FromBody] UpdateFileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateFileCommand
        {
            Id = id,
            AltText = request.AltText
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<StoredFileDto>.Success(result));
    }

    /// <summary>
    /// Move file to a collection
    /// </summary>
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(typeof(ApiResponse<StoredFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveFile(Guid id, [FromBody] MoveFileRequest request, CancellationToken cancellationToken)
    {
        var command = new MoveFileToCollectionCommand
        {
            FileId = id,
            CollectionId = request.CollectionId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<StoredFileDto>.Success(result));
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteFileCommand(id);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// List files with optional filtering and cursor-based pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<StoredFileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFiles(
        [FromQuery] Guid? collectionId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? mimeType = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListFilesQuery
        {
            CollectionId = collectionId,
            Search = search,
            MimeTypeFilter = mimeType,
            Cursor = cursor,
            Size = size
        };

        var result = await _mediator.Send(query, cancellationToken);

        var meta = new ApiMeta
        {
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };

        return Ok(ApiResponse<List<StoredFileDto>>.Success(result.Items, meta));
    }

    private static string GenerateETag(Guid id, DateTimeOffset updatedAt)
    {
        var input = $"{id}-{updatedAt.Ticks}";
        var hash = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed record UpdateFileRequest
{
    public string? AltText { get; init; }
}

public sealed record MoveFileRequest
{
    public Guid? CollectionId { get; init; }
}
