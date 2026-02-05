using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Commands
{
    public class CreateUserCommand : IRequest<UserAccountDto>
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Password { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(150)]
        public string? EmailAddress { get; set; }

        public int? Role { get; set; }
        public int? UserType { get; set; }

        // ✅ Client-related fields
        public bool IsClient { get; set; } = false;
        public int? ClientId { get; set; }

        // ✅ Optional metadata
        public int? CreatedBy { get; set; }
        // ✅ Added properties to match your entity mapping or handler usage
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public string? JWToken { get; set; }
        public string? RememberToken { get; set; }
        public int? Status { get; set; } = 1;
        public int? UpdatedBy { get; set; }

        // ✅ Optional file upload

        public UploadedFileDto? UploadFile { get; set; }
    }
}
