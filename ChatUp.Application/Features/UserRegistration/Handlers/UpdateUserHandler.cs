using BCrypt.Net;
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
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserAccountDto>
    {
        private readonly IChatDBContext _context;
        public UpdateUserHandler(IChatDBContext context) => _context = context;

        public async Task<UserAccountDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _context.UserAccounts
                    .Include(x => x.Client)
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (entity == null)
                    throw new InvalidOperationException("User not found.");

                // ✅ Update user fields
                entity.FullName = request.FullName;
                entity.EmailAddress = request.EmailAddress;
                entity.Role = request.Role;
                entity.IsClient = request.IsClient;
                entity.ClientId = request.IsClient ? request.ClientId : null;
                entity.DateUpdated = DateTime.Now;
                entity.UserType = request.UserType;
                entity.Status = request.Status;

                await _context.SaveChangesAsync(cancellationToken);

                // ✅ Manage UserClientAssignment
                if (request.IsClient && request.ClientId.HasValue)
                {
                    var existingAssignment = await _context.UserClientAssignments
                        .FirstOrDefaultAsync(x => x.UserId == entity.Id, cancellationToken);

                    if (existingAssignment == null)
                    {
                        _context.UserClientAssignments.Add(new UserClientAssignment
                        {
                            UserId = entity.Id,
                            ClientId = request.ClientId.Value
                        });
                    }
                    else
                    {
                        existingAssignment.ClientId = request.ClientId.Value;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    var existingAssignment = await _context.UserClientAssignments
                        .FirstOrDefaultAsync(x => x.UserId == entity.Id, cancellationToken);
                    if (existingAssignment != null)
                    {
                        _context.UserClientAssignments.Remove(existingAssignment);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                // ✅ Ensure ChatUser record exists or update display name
                var chatUser = await _context.ChatUsers
      .FirstOrDefaultAsync(c => c.UserId == entity.Id, cancellationToken);

                if (chatUser == null)
                {
                    chatUser = new ChatUser
                    {
                        UserId = entity.Id ?? 0,          // ✅ Set the UserId to match UserAccount
                        UserName = entity.Username,
                        DisplayName = entity.FullName
                    };
                    _context.ChatUsers.Add(chatUser);
                }
                else
                {
                    chatUser.DisplayName = entity.FullName;
                    chatUser.UserName = entity.Username;
                }

                await _context.SaveChangesAsync(cancellationToken);

                // ✅ Handle uploaded file
                if (request.UploadFile != null)
                {
                    var existingFile = await _context.UploadedFile
                        .FirstOrDefaultAsync(f => f.UserAccountId == entity.Id, cancellationToken);

                    if (existingFile == null)
                    {
                        _context.UploadedFile.Add(new UploadedFile
                        {
                            Name = request.UploadFile.Name,
                            Size = request.UploadFile.Size,
                            Base64Content = request.UploadFile.Base64Content,
                            UserAccountId = entity.Id,
                            ContentType = request.UploadFile.ContentType,
                            FileType = request.UploadFile.FileType,
                        });
                    }
                    else
                    {
                        existingFile.Name = request.UploadFile.Name;
                        existingFile.Size = request.UploadFile.Size;
                        existingFile.Base64Content = request.UploadFile.Base64Content;
                        existingFile.ContentType = request.UploadFile.ContentType;
                        existingFile.FileType = request.UploadFile.FileType;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }

                // ✅ Return updated DTO
                return new UserAccountDto
                {
                    Id = entity.Id,
                    Username = entity.Username,
                    FullName = entity.FullName,
                    EmailAddress = entity.EmailAddress,
                    Role = entity.Role,
                    IsClient = entity.IsClient,
                    ClientId = entity.ClientId,
                    ClientName = entity.Client?.ClientName
                };
            }
            catch (DbUpdateException dbEx)
            {
                // Log or handle database-specific errors
                throw new InvalidOperationException("Failed to update user in the database.", dbEx);
            }
            catch (Exception ex)
            {
                // Log or handle all other errors
                throw new InvalidOperationException("An unexpected error occurred while updating the user.", ex);
            }
        }
    }
}
