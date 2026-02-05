using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Contracts.DTOs;
using ChatUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class ContractService
    {
        private readonly ChatDBContext _context;

        public ContractService(ChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<ContractDto>> GetAllAsync()
        {
            var contracts = await _context.Contracts
               .Include(c => c.Projects)
               .Include(c => c.UserContracts)
                   .ThenInclude(uc => uc.UserAccount)
               .Include(c => c.Client)
               .AsNoTracking()
               .Select(c => new ContractDto
               {
                   Id = c.Id,
                   Title = c.Title,
                   ClientName = c.Client != null ? c.Client.ClientName : string.Empty,
                   EmailAddress = c.Client != null ? c.Client.Email : string.Empty,
                   ExpirationDate = c.ExpirationDate
               })
               .ToListAsync();

            return contracts;
        }
    }
}
