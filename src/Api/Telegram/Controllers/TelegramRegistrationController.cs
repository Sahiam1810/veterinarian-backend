using Api.Common.Security;
using Api.Telegram.Dtos;
using Application.Security.Errors;
using Application.Telegram.Registration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Telegram.Controllers;

[AllowAnonymous]
[Route("telegram/registration/complete")]
[EnableRateLimiting(RateLimitPolicies.TelegramRegistration)]
public sealed class TelegramRegistrationController(
    ISender sender,
    IWebHostEnvironment environment) : Controller
{
    private const string DevelopmentCookieName = "HuellitasTelegramRegistration";
    private const string ProductionCookieName = "__Host-HuellitasTelegramRegistration";

    [HttpGet]
    public async Task<IActionResult> Complete(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var pending = await sender.Send(
                new GetTelegramRegistrationSessionQuery(token), cancellationToken);
            if (pending.IsFailure)
            {
                return Expired();
            }

            Response.Cookies.Append(
                CookieName,
                token,
                CookieOptions(pending.Value.ExpiresAt));
            return RedirectToAction(nameof(Complete));
        }

        if (!Request.Cookies.TryGetValue(CookieName, out var cookieToken) ||
            string.IsNullOrWhiteSpace(cookieToken))
        {
            return Expired();
        }

        var result = await sender.Send(
            new GetTelegramRegistrationSessionQuery(cookieToken), cancellationToken);
        return result.IsSuccess ? View("Complete") : Expired();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        [FromForm] CompleteTelegramRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(CookieName, out var token) ||
            string.IsNullOrWhiteSpace(token))
        {
            return Expired();
        }

        if (!ModelState.IsValid)
        {
            return View("Complete", request);
        }

        var result = await sender.Send(
            new CompleteTelegramRegistrationCommand(
                token,
                request.FullName,
                request.IdentificationNumber,
                request.UserName,
                request.Password,
                request.PasswordConfirmation),
            cancellationToken);
        if (result.IsSuccess)
        {
            Response.Cookies.Delete(CookieName, DeleteCookieOptions());
            return View("Success");
        }

        if (result.Error == TelegramRegistrationErrors.InvalidOrExpired)
        {
            Response.Cookies.Delete(CookieName, DeleteCookieOptions());
            return Expired();
        }

        var field = result.Error == AuthenticationErrors.IdentificationNumberAlreadyExists
            ? nameof(request.IdentificationNumber)
            : result.Error == AuthenticationErrors.UserAlreadyExists
                ? nameof(request.UserName)
                : string.Empty;
        ModelState.AddModelError(
            field,
            result.Error == AuthenticationErrors.IdentificationNumberAlreadyExists
                ? "El número de identificación ya está registrado."
                : result.Error == AuthenticationErrors.UserAlreadyExists
                    ? "El correo o nombre de usuario ya está registrado."
                    : "No fue posible completar el registro. Inténtalo nuevamente.");
        return View("Complete", request);
    }

    private string CookieName => environment.IsDevelopment()
        ? DevelopmentCookieName
        : ProductionCookieName;

    private CookieOptions CookieOptions(DateTime expiresAt) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
        Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc))
    };

    private CookieOptions DeleteCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/"
    };

    private ViewResult Expired() => new()
    {
        ViewName = "Expired",
        StatusCode = StatusCodes.Status410Gone
    };
}
