using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.DTOs
{
    public class UploadedFileDto
    {
        public string? Name { get; set; }
        public int? Size { get; set; }
        public string? Base64Content { get; set; }
        public string? ContentType { get; set; }
        public string? FileType { get; set; }
    }
}
