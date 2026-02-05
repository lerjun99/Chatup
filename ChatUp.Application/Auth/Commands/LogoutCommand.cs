using ChatUp.Application.Auth.DTOs;
using MediatR;

namespace ChatUp.Application.Auth.Commands;


public class LogoutCommand : IRequest<bool>
{
    public int UserId { get; set; }   // which user is logging out
}