using deblog.Server.Common.Security.RateLimiting;
using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Emails;

public static class SendOtpEmailEndpoints
{
    public static RouteGroupBuilder MapSendOtpEmails(this RouteGroupBuilder group)
    {
        // 1. Onboarding / Verification OTP
        group.MapPost("/otp/onboarding", async (
            [FromBody] SendOtpEmailRequest request,
            IEmailService emailService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.To) || !request.To.Contains('@'))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Email",
                    detail: "A valid email address is required to receive verification codes."
                );
            }

            if (string.IsNullOrWhiteSpace(request.OtpCode) || request.OtpCode.Trim().Length < 4)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid OTP Code",
                    detail: "OTP code must be at least 4 characters."
                );
            }

            var recipientName = !string.IsNullOrWhiteSpace(request.RecipientName)
                ? request.RecipientName.Trim()
                : request.To.Split('@')[0];

            var result = await emailService.SendOnboardingOtpAsync(
                request.To.Trim(),
                recipientName,
                request.OtpCode.Trim(),
                ct
            );

            return Results.Ok(new EmailResponse(
                Success: result.Success,
                MessageId: result.MessageId,
                Message: result.Success
                    ? "Onboarding verification code dispatched successfully."
                    : result.ErrorMessage ?? "Email dispatch failed or operated in offline mode."
            ));
        })
        .WithName("SendOnboardingOtp")
        .WithSummary("Dispatch onboarding/account verification OTP code via email")
        .RequireRateLimiting(RateLimitingPolicies.OtpSpam);

        // 2. Password Reset OTP
        group.MapPost("/otp/password-reset", async (
            [FromBody] SendOtpEmailRequest request,
            IEmailService emailService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.To) || !request.To.Contains('@'))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Email",
                    detail: "A valid email address is required for password recovery."
                );
            }

            if (string.IsNullOrWhiteSpace(request.OtpCode) || request.OtpCode.Trim().Length < 4)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid OTP Code",
                    detail: "OTP code must be at least 4 characters."
                );
            }

            var recipientName = !string.IsNullOrWhiteSpace(request.RecipientName)
                ? request.RecipientName.Trim()
                : request.To.Split('@')[0];

            var result = await emailService.SendPasswordResetOtpAsync(
                request.To.Trim(),
                recipientName,
                request.OtpCode.Trim(),
                ct
            );

            return Results.Ok(new EmailResponse(
                Success: result.Success,
                MessageId: result.MessageId,
                Message: result.Success
                    ? "Password reset security code dispatched successfully."
                    : result.ErrorMessage ?? "Email dispatch failed or operated in offline mode."
            ));
        })
        .WithName("SendPasswordResetOtp")
        .WithSummary("Dispatch password reset security OTP code via email")
        .RequireRateLimiting(RateLimitingPolicies.OtpSpam);

        return group;
    }
}
