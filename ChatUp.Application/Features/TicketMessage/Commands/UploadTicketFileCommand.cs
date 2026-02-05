using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Commands
{
    public record UploadTicketFileCommand(
        int TicketId,
        int UploadedById,
        string FileName,
        string FileType,
        string Base64Content,
        int? TicketMessageId = null
  ) : IRequest<int>;
}
