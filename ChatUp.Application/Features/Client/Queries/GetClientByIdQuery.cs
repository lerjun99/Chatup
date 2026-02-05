using ChatUp.Application.Features.Client.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Client.Queries
{
    public record GetClientByIdQuery(int Id) : IRequest<ClientDto?>;
}
