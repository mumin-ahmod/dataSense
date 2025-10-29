using MediatR;
using DataSenseAPI.Application.Abstractions;

namespace DataSenseAPI.Application.Commands.GenerateSql;

public sealed class GenerateSqlCommandHandler : IRequestHandler<GenerateSqlCommand, string>
{
    private readonly IBackendSqlGeneratorService _sqlGenerator;
    private readonly ISqlSafetyValidator _safetyValidator;

    public GenerateSqlCommandHandler(
        IBackendSqlGeneratorService sqlGenerator,
        ISqlSafetyValidator safetyValidator)
    {
        _sqlGenerator = sqlGenerator;
        _safetyValidator = safetyValidator;
    }

    public async Task<string> Handle(GenerateSqlCommand request, CancellationToken cancellationToken)
    {
        var sql = await _sqlGenerator.GenerateSqlAsync(request.NaturalQuery, request.Schema, request.DbType);
        var sanitized = _safetyValidator.SanitizeQuery(sql);
        if (!_safetyValidator.IsSafe(sanitized))
        {
            throw new InvalidOperationException("Generated SQL query failed safety validation and could not be automatically corrected");
        }
        return sanitized;
    }
}


