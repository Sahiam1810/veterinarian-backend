using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.HospitalizationStays.Dtos;
using Application.HospitalizationStays.UseCases;
using Application.MedicationOrders.UseCases;
using Application.Permissions.Claims;
using Application.ProcedureOrders.UseCases;
using Application.Supplies.UseCases;
using Application.SupplyConsumptions.UseCases;
using Domain.MedicationOrders.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.Supplies.Entities;
using Domain.SupplyConsumptions.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Api.Tests.Support;

public sealed class ModulePermissionsApiFactory : WebApplicationFactory<AuthController>
{
    private const string Issuer = "Veterinaria.Api.Security.Tests";
    private const string Audience = "Veterinaria.Client.Security.Tests";
    private const string KeyId = "module-permissions-test-key";
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();

    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public ModulePermissionsApiFactory()
    {
        var environment = new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] = "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Chat__ResolvedEscalationStatusId"] = "85000000-0000-0000-0000-000000000004",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = Issuer,
            ["Jwt__Audience"] = Audience,
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = KeyId,
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0"
        };

        foreach (var setting in environment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        ConfigureDefaultSenderBehaviors();
    }

    private void ConfigureDefaultSenderBehaviors()
    {
        Sender.Send(Arg.Any<GetAllSuppliesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Supply>());
        Sender.Send(Arg.Any<GetSupplyByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Supply("Gasa", "Unidad", 5000m, 100m, true));
        Sender.Send(Arg.Any<CreateSupplyCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Supply("Gasa", "Unidad", 5000m, 100m, true));

        Sender.Send(Arg.Any<GetPendingMedicationOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MedicationOrder>());
        Sender.Send(Arg.Any<GetPendingProcedureOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProcedureOrder>());

        // Los endpoints de Hospitalización devuelven DTOs (nunca la entidad de dominio).
        Sender.Send(Arg.Any<GetHospitalizationStayByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ApiHospitalizationStayDto(
                Guid.NewGuid(), Guid.NewGuid(), "Firulais", "Ana Dueña", null,
                DateTime.UtcNow, null, "Activa", "Motivo de prueba", Guid.NewGuid(), "Dra. Ana", false, null));
        Sender.Send(Arg.Any<GetAllActiveHospitalizationStaysQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApiHospitalizationStayDto>)Array.Empty<ApiHospitalizationStayDto>());
        Sender.Send(Arg.Any<GetHospitalizationAdmissionOptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<HospitalizationAdmissionOptionDto>)[
                new(Guid.NewGuid(), "Luna", "Juan Pérez")]);
        Sender.Send(Arg.Any<GetHospitalizationStaysByPetQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApiHospitalizationStayDto>)Array.Empty<ApiHospitalizationStayDto>());
        Sender.Send(Arg.Any<GetHospitalizationNotesByStayQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApiHospitalizationNoteDto>)Array.Empty<ApiHospitalizationNoteDto>());
        Sender.Send(Arg.Any<GetHospitalizationStaffQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<HospitalizationStaffUserDto>)Array.Empty<HospitalizationStaffUserDto>());

        Sender.Send(Arg.Any<GetSupplyConsumptionsByStayIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SupplyConsumption>());
        Sender.Send(Arg.Any<GetSupplyConsumptionTotalByStayIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var dummyConsumption = (SupplyConsumption)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SupplyConsumption));
        typeof(SupplyConsumption).GetProperty("Id")?.SetValue(dummyConsumption, Guid.NewGuid());
        typeof(SupplyConsumption).GetProperty("HospitalizationStayId")?.SetValue(dummyConsumption, Guid.NewGuid());
        Sender.Send(Arg.Any<RegisterSupplyConsumptionCommand>(), Arg.Any<CancellationToken>())
            .Returns(dummyConsumption);
    }

    public HttpClient CreateGuestClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    public HttpClient CreateClientWithPermissions(params string[] permissions)
    {
        var client = CreateGuestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(permissions));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISender>();
            services.AddSingleton(Sender);
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton(UnitOfWork);
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            foreach (var setting in originalEnvironment)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }
        }
    }

    public static string CreateToken(params string[] permissions)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(
            Convert.FromBase64String(Keys.PrivateKeyPemBase64)));
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = KeyId,
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false
            }
        };
        var now = DateTime.UtcNow;
        var userGuid = Guid.NewGuid().ToString();
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, userGuid),
                new Claim(ClaimTypes.NameIdentifier, userGuid),
                new Claim("person_id", userGuid),
                new Claim("role_id", Guid.NewGuid().ToString())
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

        if (permissions != null && permissions.Length > 0)
        {
            var claimValues = permissions.Select(p =>
            {
                var parts = p.Split(':');
                return PermissionClaimValue.Create(parts[0], parts[1]);
            }).ToArray();

            token.Payload[PermissionClaimValue.ClaimType] = claimValues;
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
