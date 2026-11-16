using MediatR;

namespace ChatUp.Application.Features.AutoReply
{
    /// <summary>
    /// Auto-reply rows the user may see: an admin sees all of them, a support agent the default row
    /// plus the rows of the clients they are assigned to.
    /// </summary>
    public record GetAutoRepliesQuery(int UserId) : IRequest<List<AutoReplyDto>>;

    public class SaveAutoReplyCommand : IRequest<AutoReplyDto>
    {
        public int UserId { get; set; }
        public AutoReplyDto Data { get; set; } = new();
    }

    public record DeleteAutoReplyCommand(int Id, int UserId) : IRequest<bool>;
}
