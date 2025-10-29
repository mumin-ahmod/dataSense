using MediatR;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Commands.InterpretResults;

public sealed record InterpretResultsCommand(
    string OriginalQuery,
    string SqlQuery,
    object Results
) : IRequest<InterpretationData>;


