using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserRegistration.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserAccountDto>
    {
        private readonly IChatDBContext _context;
        private readonly ICryptography _crypto;
        public CreateUserHandler(IChatDBContext context, ICryptography crypto)
        {
            _crypto = crypto;
            _context = context;
        }

        public async Task<UserAccountDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Check for duplicate username
            if (await _context.UserAccounts.AnyAsync(x => x.Username == request.Username, cancellationToken))
                throw new InvalidOperationException("Username already exists.");

            // 2. Check for duplicate email (active/non-deleted accounts only)
            if (await _context.UserAccounts.AnyAsync(x => x.EmailAddress.ToLower() == request.EmailAddress.ToLower() && x.IsDeleted == 0, cancellationToken))
                throw new InvalidOperationException("Email address already exists.");

            // 3. Validate ClientId if user is a client
            if (request.IsClient)
            {
                if (request.ClientId == null)
                    throw new InvalidOperationException("Client user must be linked to a Client record.");

                var clientExists = await _context.Client.AnyAsync(c => c.Id == request.ClientId, cancellationToken);
                if (!clientExists)
                    throw new InvalidOperationException($"Client with ID {request.ClientId} does not exist.");
            }

            // 4. Create new user account
            var user = new UserAccount
            {
                Username = request.Username,
                Password = _crypto.Encrypt(request.Password),
                FullName = request.FullName,
                EmailAddress = request.EmailAddress,
                Role = request.Role,
                IsClient = request.IsClient,
                ClientId = request.ClientId,
                IsActive = 1,
                DateCreated = DateTime.Now,
                IsDeleted = 0,
                UserType = request.UserType,
                IsFirstLogIn = true,
                Status = request.Status
            };
            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Create UserClientAssignment if applicable
            if (request.ClientId.HasValue)
            {
                var assignment = new UserClientAssignment
                {
                    UserId = user.Id,
                    ClientId = request.ClientId.Value
                };
                _context.UserClientAssignments.Add(assignment);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 6. Create ChatUser
            var chatUser = new ChatUser
            {
                UserId = user.Id ?? 0,
                UserName = user.Username,
                DisplayName = user.FullName
            };
            _context.ChatUsers.Add(chatUser);
            await _context.SaveChangesAsync(cancellationToken);

            // 7. Handle uploaded file
            if (request.UploadFile != null)
            {
                var uploadedFile = new UploadedFile
                {
                    Name = request.UploadFile.Name,
                    Size = request.UploadFile.Size,
                    Base64Content = request.UploadFile.Base64Content,
                    UserAccountId = user.Id,
                    ContentType = request.UploadFile.ContentType,
                    FileType = request.UploadFile.FileType,
                };
                _context.UploadedFile.Add(uploadedFile);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 8. Get client name if available
            var clientEntity = request.ClientId != null
                ? await _context.Client.FindAsync(new object[] { request.ClientId }, cancellationToken)
                : null;

            // 9. Return DTO
            return new UserAccountDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                EmailAddress = user.EmailAddress,
                Role = user.Role,
                IsClient = user.IsClient,
                ClientId = user.ClientId,
                ClientName = clientEntity?.ClientName
            };
        }
    }

}
