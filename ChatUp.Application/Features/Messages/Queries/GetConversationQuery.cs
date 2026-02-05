using ChatUp.Application.Auth.DTOs;
using MediatR;

namespace ChatUp.Application.Features.Messages.Queries
{
    public record GetConversationQuery(int user1Id, int user2Id, int skip, int take);
}

