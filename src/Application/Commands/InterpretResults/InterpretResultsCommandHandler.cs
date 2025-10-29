using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Commands.InterpretResults;

public sealed class InterpretResultsCommandHandler : IRequestHandler<InterpretResultsCommand, InterpretationData>
{
    private readonly IBackendResultInterpreterService _interpreterService;

    public InterpretResultsCommandHandler(IBackendResultInterpreterService interpreterService)
    {
        _interpreterService = interpreterService;
    }

    public async Task<InterpretationData> Handle(InterpretResultsCommand request, CancellationToken cancellationToken)
    {
        var mapped = new Domain.Models.InterpretResultsRequest
        {
            OriginalQuery = request.OriginalQuery,
            SqlQuery = request.SqlQuery,
            Results = request.Results
        };

        return await _interpreterService.InterpretResultsAsync(mapped);
    }
}


