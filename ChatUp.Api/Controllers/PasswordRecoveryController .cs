
using ChatUp.Application.Features.EmailOTP.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    [ApiController]
    [Route("api/password-recovery")]
    public class PasswordRecoveryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PasswordRecoveryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok("OTP sent successfully.") : BadRequest("Failed to send OTP.");
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok("OTP resent successfully.") : BadRequest("Failed to resend OTP.");
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok("OTP verified.") : BadRequest("Invalid or expired OTP.");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangeLogPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok("Password changed successfully.") : BadRequest("Password change failed.");
        }
    }
}
