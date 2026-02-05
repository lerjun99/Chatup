using AutoMapper;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Mappings
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<Ticket, TicketDto>()
             .ForMember(dest => dest.TicketNo, opt => opt.MapFrom(s => s.TicketNo))
             .ForMember(dest => dest.IssueTitle, opt => opt.MapFrom(s => s.IssueTitle))
             .ForMember(dest => dest.DateReceived, opt => opt.MapFrom(s => s.DateReceived))
             .ForMember(dest => dest.DueDate, opt => opt.MapFrom(s => s.DueDate))
             .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(s => s.ProjectId))
             .ForMember(dest => dest.Status, opt => opt.MapFrom(s => s.Status))
             .ForMember(dest => dest.Messages, opt => opt.MapFrom(s => s.Messages))
             .ForMember(dest => dest.SupportedById, opt => opt.MapFrom(src => src.SupportedById))
            .ForMember(dest => dest.RequestedById, opt => opt.MapFrom(src => src.RequestedById));

            CreateMap<TicketMessage, MessageDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Content, o => o.MapFrom(s => s.Content))
                .ForMember(d => d.DateCreated, o => o.MapFrom(s => s.DateCreated))
                .ForMember(d => d.IsUser, o => o.MapFrom(s => s.IsUser))
                .ForMember(d => d.SenderId, o => o.MapFrom(s => s.SenderId));
        }
    }
}
