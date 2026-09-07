using Api.Common.Security;
using Api.Telegram.Dtos;
using Application.Clients.Errors;
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
                request.PhoneNumber,
                request.Address),
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

        // Codes del núcleo RegisterOwner / catálogo auth-clients.
        var field = result.Error.Code switch
        {
            var c when c == AuthenticationErrors.IdentificationNumberAlreadyExists.Code =>
                nameof(request.IdentificationNumber),
            var c when c == ClientErrorCodes.PhoneAlreadyInUse =>
                nameof(request.PhoneNumber),
            _ => string.Empty
        };
        var message = result.Error.Code switch
        {
            var c when c == AuthenticationErrors.IdentificationNumberAlreadyExists.Code =>
                "El número de identificación ya está registrado.",
            var c when c == AuthenticationErrors.UserAlreadyExists.Code =>
                "El correo ya está registrado.",
            var c when c == ClientErrorCodes.PhoneAlreadyInUse =>
                "El teléfono ya está registrado.",
            _ => "No fue posible completar el registro. Inténtalo nuevamente."
        };
        ModelState.AddModelError(field, message);
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
