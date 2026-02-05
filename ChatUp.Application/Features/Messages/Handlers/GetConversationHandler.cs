using BCrypt.Net;
using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Auth.DTOs;
using ChatUp.Application.Auth.DTOs;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using ChatUp.Application.Features.Messages.Queries;
namespace ChatUp.Application.Features.Messages.Handlers
{
    public class GetConversationHandler
    {
        private readonly IMessageRepository _repository;

        public GetConversationHandler(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ChatMessage>> Handle(GetConversationQuery query)
        {
            return await _repository.GetConversationAsync(query.user1Id, query.user2Id, query.skip, query.take);
        }
    }
}