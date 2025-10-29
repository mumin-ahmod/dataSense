using MediatR;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Commands.GenerateSql;

public sealed record GenerateSqlCommand(
    string NaturalQuery,
    DatabaseSchema Schema,
    string DbType
) : IRequest<string>;


