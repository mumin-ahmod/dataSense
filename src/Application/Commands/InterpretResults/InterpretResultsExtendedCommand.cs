using MediatR;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Commands.InterpretResults;

public sealed record InterpretResultsExtendedCommand(
    string OriginalQuery,
    string SqlQuery,
    object Results,
    string? AdditionalContext
) : IRequest<InterpretationData>;

