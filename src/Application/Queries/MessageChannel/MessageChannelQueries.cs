using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using MessageChannelEntity = DataSenseAPI.Domain.Models.MessageChannel;

namespace DataSenseAPI.Application.Queries.MessageChannel;

// Get All Message Channels Query
public sealed record GetAllMessageChannelsQuery() : IRequest<List<MessageChannelEntity>>;

public sealed class GetAllMessageChannelsQueryHandler : IRequestHandler<GetAllMessageChannelsQuery, List<MessageChannelEntity>>
{
    private readonly IMessageChannelRepository _messageChannelRepository;

    public GetAllMessageChannelsQueryHandler(IMessageChannelRepository messageChannelRepository)
    {
        _messageChannelRepository = messageChannelRepository;
    }

    public async Task<List<MessageChannelEntity>> Handle(GetAllMessageChannelsQuery request, CancellationToken cancellationToken)
    {
        return await _messageChannelRepository.GetAllAsync();
    }
}

