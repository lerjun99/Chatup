using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Commands
{
    public class UpdateUserCommand : IRequest<UserAccountDto>
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string? EmailAddress { get; set; }
        public int? Role { get; set; }
        public bool IsClient { get; set; }
        public int? ClientId { get; set; }
        public int? UserType { get; set; }
        public int? Status { get; set; }
        public bool IsActive { get; set; } = true;
        public UploadedFileDto? UploadFile { get; set; }
    }
}
