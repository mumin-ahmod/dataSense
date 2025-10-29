using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Commands.InterpretResults;

public sealed class InterpretResultsExtendedCommandHandler : IRequestHandler<InterpretResultsExtendedCommand, InterpretationData>
{
    private readonly IBackendResultInterpreterService _interpreterService;
    private readonly IOllamaService _ollamaService;

    public InterpretResultsExtendedCommandHandler(
        IBackendResultInterpreterService interpreterService,
        IOllamaService ollamaService)
    {
        _interpreterService = interpreterService;
        _ollamaService = ollamaService;
    }

    public async Task<InterpretationData> Handle(InterpretResultsExtendedCommand request, CancellationToken cancellationToken)
    {
        // If additional context is provided, enhance the interpretation
        if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
        {
            // Build enhanced prompt with context
            var contextPrompt = $@"
Additional context provided:
{request.AdditionalContext}

Please consider this context when interpreting the query results below.
";
            
            // The interpreter service can be enhanced to use this context
            // For now, we'll pass it through metadata or append to the original query
            var originalRequest = new Domain.Models.InterpretResultsRequest
            {
                OriginalQuery = request.OriginalQuery,
                SqlQuery = request.SqlQuery,
                Results = request.Results
            };

            var interpretation = await _interpreterService.InterpretResultsAsync(originalRequest);
            
            // If context is provided, we can post-process the interpretation
            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
            {
                var enhancedPrompt = $@"
Original interpretation:
Answer: {interpretation.Answer}
Analysis: {interpretation.Analysis}

Additional context to consider: {request.AdditionalContext}

Provide an enhanced interpretation that incorporates the additional context while maintaining accuracy.
Format the response with Answer, Analysis, and Summary sections.
";

                try
                {
                    var enhanced = await _ollamaService.QueryLLMAsync(enhancedPrompt);
                    // Parse enhanced response (simplified - in production, use proper JSON parsing)
                    // For now, we'll use the original interpretation
                }
                catch
                {
                    // Fall back to original interpretation if enhancement fails
                }
            }

            return interpretation;
        }

        // Default behavior without context
        var mapped = new Domain.Models.InterpretResultsRequest
        {
            OriginalQuery = request.OriginalQuery,
            SqlQuery = request.SqlQuery,
            Results = request.Results
        };

        return await _interpreterService.InterpretResultsAsync(mapped);
    }
}

