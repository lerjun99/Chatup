using ChatUp.Application.Auth.DTOs;
using MediatR;

namespace ChatUp.Application.Auth.Commands;


public record LoginCommand(string Username, string Password) : IRequest<AuthResponseDto<LoginResponseDto>>;