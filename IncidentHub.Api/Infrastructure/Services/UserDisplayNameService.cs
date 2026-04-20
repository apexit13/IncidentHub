using IncidentHub.Api.Features.Users.Queries.GetUserById;
using MediatR;

namespace IncidentHub.Api.Infrastructure.Services
{
    public interface IUserDisplayNameService
    {
        Task<string> GetDisplayNameAsync(string userId);
    }

    public class UserDisplayNameService : IUserDisplayNameService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserDisplayNameService> _logger;

        public UserDisplayNameService(IMediator mediator, ILogger<UserDisplayNameService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<string> GetDisplayNameAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return "Unassigned";

            if (userId == "auth0|test-user-id")
                return "Test User";

            try
            {
                var query = new GetUserByIdQuery(userId);
                var user = await _mediator.Send(query);

                return user?.Name ?? user?.Nickname ?? user?.Email ?? userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting display name for user {UserId}", userId);
                return userId;
            }
        }
    }
}