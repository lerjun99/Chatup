using AutoMapper;
using ChatUp.Application.Features.Client.DTOs;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Client, ClientDto>().ReverseMap();
            CreateMap<Ticket, Features.Ticket.DTOs.TicketDto>().ReverseMap();
            CreateMap<TicketUpload, TicketUploadDto>().ReverseMap();
        }
    }
}
