using DocumentFormat.OpenXml.Spreadsheet;
using FitHolic.Class;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using pearl_leasing.Class;
using pearl_leasing.Data;
using pearl_leasing.DTO;
using pearl_leasing.Helpers;
using pearl_leasing.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace pearl_leasing.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {

        private readonly PearlLeasingDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly GenerateTokenJwt _generateToken;
        private readonly SendEmailOtp _sendOTP;

        public AuthController(PearlLeasingDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _sendOTP = new SendEmailOtp(configuration);
            _generateToken = new GenerateTokenJwt(context, configuration);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(DTO.SuccessfulResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto tokenDto)
        {
            if (tokenDto == null) return BadRequest("Invalid client request");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == tokenDto.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return Unauthorized("Session expired. Please log in again.");
            }

            var newAccessToken = _generateToken.GenerateJwtToken(user); var newRefreshToken = TokenHelper.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7); await _context.SaveChangesAsync();

                return Ok(new DTO.SuccessfulResponse<object>("Tokens refreshed", new { AccessToken = newAccessToken, RefreshToken = newRefreshToken }));
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(DTO.SuccessfulResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return BadRequest("Invalid user session claim.");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdClaim));

                if (user == null)
                {
                    return NotFound("User not found.");
                }

                user.RefreshToken = string.Empty;
                user.RefreshTokenExpiryTime = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new DTO.SuccessfulResponse<object>("Logged out successfully. Session revoked."));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred during logout.");
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(DTO.SuccessfulResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validator = new LoginValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage
                ));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user.IsTwoFactorEnabled)
            {
                var random = new Random();
                string generatedOtp = random.Next(100000, 999999).ToString();

                var newOtp = new UserOTP
                {
                    UserId = user.Id,
                    OtpCode = generatedOtp,
                    ExpiryTime = DateTime.Now.AddMinutes(5),
                    IsUsed = false
                };
                _context.UserOtps.Add(newOtp);
                await _context.SaveChangesAsync();

                string maskedPhone = !string.IsNullOrEmpty(user.PhoneNumber)
                ? StringExtensions.MaskContactNo(user.PhoneNumber)
                : "No Phone Registered";

                await _sendOTP.SendOTP(user.Email, generatedOtp);

                int atIndex = user.Email.IndexOf("@");
                string maskedEmail = user.Email.Substring(0, Math.Min(2, atIndex)) + "******" + user.Email.Substring(atIndex);

                return Ok(new DTO.SuccessfulResponse<DTO.LoginSuccessResponse>("OTP sent to your registered email.", new DTO.LoginSuccessResponse
                {
                    Message = "OTP sent to your registered email.",
                    EmailHint = maskedEmail,
                    UserId = user.Id
                }));
            }

            if (_generateToken == null)
            {
                return StatusCode(500, "Token service is not initialized.");
            }

            var token = _generateToken.GenerateJwtToken(user);
            return Ok(new DTO.SuccessfulResponse<object>("Login successful", new { Requires2FA = false, Token = token }));
        }

        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(DTO.SuccessfulResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var currentUtcTime = DateTime.Now;
            var latestOtp = await _context.UserOtps
                .Where(o => o.UserId == request.UserId && o.IsUsed == false && o.ExpiryTime >= currentUtcTime)
                .OrderByDescending(o => o.ExpiryTime)
                .FirstOrDefaultAsync();

            var validator = new VerifyOtpValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();

                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage
                ));
            }

            if (latestOtp != null)
                latestOtp.IsUsed = true;

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(request.UserId);

            var accessToken = _generateToken.GenerateJwtToken(user);
            var refreshToken = TokenHelper.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new DTO.SuccessfulResponse<object>("Login Successful!", new { AccessToken = accessToken, RefreshToken = refreshToken }));
        }
    }
}
