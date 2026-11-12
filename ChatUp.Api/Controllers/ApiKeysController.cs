using ChatUp.Application.Common.Security;
using ChatUp.Domain.Entities;
using ChatUp.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    /// <summary>
    /// API key management. Listing, creating, revealing, rotating and revoking keys need an
    /// Admin key, or any key sent together with a signed-in administrator's X-User-Token.
    /// </summary>
    [Route("[controller]/[action]")]
    [ApiController]
    public class ApiKeysController : ControllerBase
    {
        private readonly ApiKeyService _keys;

        public ApiKeysController(ApiKeyService keys)
        {
            _keys = keys;
        }

        private int? CallerKeyId =>
            int.TryParse(User.FindFirst(ApiKeyDefaults.KeyIdClaim)?.Value, out var id) ? id : null;

        private int? CallerUserId =>
            int.TryParse(User.FindFirst(ApiKeyDefaults.UserIdClaim)?.Value, out var id) ? id : null;

        /// <summary>Which key is this? Used by the UI's "Test connection" and "Connect a key by value".</summary>
        [HttpGet]
        public async Task<IActionResult> WhoAmI()
        {
            var id = CallerKeyId;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? nameof(ApiKeyRole.ApiUser);

            // Legacy ApiTokenModel tokens have no ApiKeys row (negative ids)
            if (id is null or <= 0)
                return Ok(new { Id = id, Name = User.Identity?.Name, Role = role, IsLegacy = true });

            var info = await _keys.GetAsync(id.Value);
            return info == null ? Unauthorized() : Ok(info);
        }

        [HttpGet]
        [Authorize(Policy = ApiKeyDefaults.AdminPolicy)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeRevoked = false) =>
            Ok(await _keys.ListAsync(includeRevoked));

        [HttpPost]
        [Authorize(Policy = ApiKeyDefaults.AdminPolicy)]
        public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Give the key a name so you can recognise it later." });

            if (!Enum.IsDefined(request.Role))
                return BadRequest(new { message = "Unknown role." });

            var created = await _keys.CreateAsync(request.Name, request.Role, request.ExpiresInDays, CallerUserId);
            return Ok(created);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = ApiKeyDefaults.AdminPolicy)]
        public async Task<IActionResult> Reveal(int id)
        {
            var secret = await _keys.RevealAsync(id);
            return secret == null ? NotFound() : Ok(secret);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = ApiKeyDefaults.AdminPolicy)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateApiKeyRequest request)
        {
            var updated = await _keys.UpdateAsync(id, request.Name, request.ExpiresInDays, request.ClearExpiry);
            return updated == null ? NotFound() : Ok(updated);
        }

        /// <summary>Issues a new value for the key; the old value stops working immediately.</summary>
        [HttpPost("{id:int}")]
        [Authorize(Policy = ApiKeyDefaults.AdminPolicy)]
        public async Task<IActionResult> Rotate(int id)
        {
            var rotated = await _keys.RotateAsync(id);
            return rotated == null ? NotFound() : Ok(rotated);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = ApiKeyDefaults.AdminPolicy)]
        public async Task<IActionResult> Revoke(int id)
        {
            // Revoking the key this request was made with would lock the caller out
            if (id == CallerKeyId)
                return Conflict(new { message = "This key is the one in use for this request. Connect the UI to another key before revoking it." });

            return await _keys.RevokeAsync(id, CallerUserId) ? NoContent() : NotFound();
        }
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public ApiKeyRole Role { get; set; } = ApiKeyRole.ApiUser;
        public int? ExpiresInDays { get; set; }
    }

    public class UpdateApiKeyRequest
    {
        public string? Name { get; set; }
        public int? ExpiresInDays { get; set; }
        public bool ClearExpiry { get; set; }
    }
}
