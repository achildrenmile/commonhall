using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Files.Commands;
using MediatR;

namespace CommonHall.Application.Features.Files.Handlers;

public sealed class UploadMultipleFilesCommandHandler : IRequestHandler<UploadMultipleFilesCommand, List<StoredFileDto>>
{
    private readonly IMediator _mediator;

    public UploadMultipleFilesCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<StoredFileDto>> Handle(UploadMultipleFilesCommand request, CancellationToken cancellationToken)
    {
        var results = new List<StoredFileDto>();

        foreach (var file in request.Files)
        {
            var command = new UploadFileCommand
            {
                Stream = file.Stream,
                OriginalName = file.OriginalName,
                MimeType = file.MimeType,
                CollectionId = request.CollectionId
            };

            var result = await _mediator.Send(command, cancellationToken);
            results.Add(result);
        }

        return results;
    }
}
