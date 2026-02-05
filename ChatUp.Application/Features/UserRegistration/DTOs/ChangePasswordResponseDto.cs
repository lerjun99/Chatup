using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.DTOs
{
    public class ChangePasswordResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? TemporaryPassword { get; set; }
        public string? Message { get; set; }
    }
}
