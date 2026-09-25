using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class GetAppointmentReceiptQueryHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
    private readonly IClientRepository _clients = Substitute.For<IClientRepository>();
    private readonly GetAppointmentReceiptQueryHandler _handler;

    public GetAppointmentReceiptQueryHandlerTests()
    {
        _unitOfWork.AppointmentsRepository.Returns(_appointments);
        _unitOfWork.ClientsRepository.Returns(_clients);
        _handler = new GetAppointmentReceiptQueryHandler(_unitOfWork);
    }

    private static Appointment BuildAppointment() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 9, 24, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 24, 9, 30, 0, DateTimeKind.Utc),
            null);

    // Las navegaciones (ClientPet, Service, ClientPet.Pet) tienen setter `private`
    // y el constructor de ClientPetEntity solo fija los FK (ClientId/PetId), no
    // las navegaciones -- eso lo hace EF al cargar con .Include(). Fuera del
    // repositorio real no hay otra forma de simularlas en un test unitario.
    private static void Attach(Appointment appointment, ClientPetEntity? clientPet, PetEntity? pet, Service? service)
    {
        if (clientPet is not null && pet is not null)
        {
            typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(clientPet, pet);
        }

        var type = typeof(Appointment);
        type.GetProperty(nameof(Appointment.ClientPet))!.SetValue(appointment, clientPet);
        type.GetProperty(nameof(Appointment.Service))!.SetValue(appointment, service);
    }

    [Fact]
    public async Task Throws_not_found_when_the_appointment_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _appointments.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetAppointmentReceiptQuery(missingId), CancellationToken.None));
    }

    [Fact]
    public async Task Falls_back_to_placeholder_text_when_navigations_are_not_loaded()
    {
        var appointment = BuildAppointment();
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var result = await _handler.Handle(new GetAppointmentReceiptQuery(appointment.Id), CancellationToken.None);

        Assert.Equal("Paciente Desconocido", result.PetName);
        Assert.Equal("Dueño Desconocido", result.OwnerName);
        Assert.Null(result.OwnerPhone);
        Assert.Equal("Servicio Desconocido", result.ServiceName);
        Assert.Equal(0m, result.ServicePrice);
        await _clients.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_the_receipt_data_from_the_loaded_appointment()
    {
        var species = new SpeciesEntity("Perro");
        var race = new RaceEntity("Labrador", species);
        var pet = new PetEntity("Firulais", 3, "M", 20m, null, species, race);
        var client = new ClientEntity("Ana Dueña", "ana@huellitas.test", "1234567890", "3001234567", "Calle 1");
        var clientPet = new ClientPetEntity(client, pet, isPrimaryOwner: true);
        var service = new Service(Guid.NewGuid(), "Consulta general", 30, 45000m);

        var appointment = BuildAppointment();
        Attach(appointment, clientPet, pet, service);
        appointment.RegisterPayment(service.Price);

        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);
        _clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(client);

        var result = await _handler.Handle(new GetAppointmentReceiptQuery(appointment.Id), CancellationToken.None);

        Assert.Equal("Firulais", result.PetName);
        Assert.Equal("Ana Dueña", result.OwnerName);
        Assert.Equal("3001234567", result.OwnerPhone);
        Assert.Equal("Consulta general", result.ServiceName);
        Assert.Equal(45000m, result.ServicePrice);
        Assert.Equal(45000m, appointment.PaidAmount);
        Assert.Equal(appointment.ScheduledStart, result.ScheduledStart);
        Assert.True(result.IsPaid);
    }
}
