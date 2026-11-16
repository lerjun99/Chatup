using MediatR;

namespace ChatUp.Application.Features.User.Commands
{
    /// <summary>Assigns several support agents to one client at once.</summary>
    public class AssignClientSupportsCommand : IRequest<AssignClientSupportsResult>
    {
        public int ClientId { get; set; }
        public List<int> UserIds { get; set; } = new();
    }

    public class AssignClientSupportsResult
    {
        public int Added { get; set; }
        public int AlreadyAssigned { get; set; }

        /// <summary>Ids that are not active support accounts.</summary>
        public int Skipped { get; set; }
    }
}
