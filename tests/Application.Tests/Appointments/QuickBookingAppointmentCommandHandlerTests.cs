using Application.Appointments.UseCases;
using Application.Clients.Errors;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.UseCases;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Users.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class QuickBookingAppointmentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IVeterinarianAbsenceRepository _absences = Substitute.For<IVeterinarianAbsenceRepository>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public QuickBookingAppointmentCommandHandlerTests()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task QuickBooking_Creates_Client_Pet_And_Appointment_Without_Dummy_Data()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();

        var service = new Service(Guid.NewGuid(), "Consulta General", 30, 50000m, true);
        _unitOfWork.ServicesRepository.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(service);

        _unitOfWork.ClientsRepository.GetByPhoneAsync("3001234567", Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var species = new SpeciesEntity("Canino");
        _unitOfWork.SpeciesRepository.GetByIdAsync(speciesId, Arg.Any<CancellationToken>())
            .Returns(species);

        var availability = new Availability(vetId, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(18, 0), true);
        _unitOfWork.AvailabilitiesRepository.LockByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(availability);
        _unitOfWork.AvailabilitiesRepository.GetAllByVeterinarianIdAsync(vetId, Arg.Any<CancellationToken>())
            .Returns(new[] { availability });

        var statusAgendada = new StatusAppointment("AGENDADA", "Agendada");
        _unitOfWork.StatusAppointmentsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { statusAgendada });

        ClientEntity? capturedClient = null;
        await _unitOfWork.ClientsRepository.AddAsync(Arg.Do<ClientEntity>(c => capturedClient = c), Arg.Any<CancellationToken>());

        PetEntity? capturedPet = null;
        await _unitOfWork.PetsRepository.AddAsync(Arg.Do<PetEntity>(p => capturedPet = p), Arg.Any<CancellationToken>());

        Appointment? capturedAppointment = null;
        await _unitOfWork.AppointmentsRepository.AddAsync(Arg.Do<Appointment>(a => capturedAppointment = a), Arg.Any<CancellationToken>());

        var handler = new QuickBookingAppointmentCommandHandler(_unitOfWork, _absences, _timeProvider);

        var nextMonday = DateTime.UtcNow.Date.AddDays(((int)DayOfWeek.Monday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7 + 7).AddHours(15);
        var command = new QuickBookingAppointmentCommand(
            ClientId: null,
            ClientPhoneNumber: "3001234567",
            ClientFullName: "Carlos Pérez",
            PetName: "Fido",
            SpeciesId: speciesId,
            ServiceId: serviceId,
            VeterinarianId: vetId,
            ScheduledStart: nextMonday
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(capturedClient);
        Assert.Null(capturedClient!.Email);
        Assert.Null(capturedClient.IdentificationNumber);
        Assert.Equal("Carlos Pérez", capturedClient.FullName.Value);
        Assert.Equal("3001234567", capturedClient.PhoneNumber.Value);

        Assert.NotNull(capturedPet);
        Assert.Null(capturedPet!.Age);
        Assert.Null(capturedPet.Weight);
        Assert.Null(capturedPet.RaceId);
        Assert.Equal("Fido", capturedPet.Name.Value);

        Assert.NotNull(capturedAppointment);
        Assert.Equal(serviceId, capturedAppointment!.ServiceId);
        Assert.Equal(vetId, capturedAppointment.VeterinarianId);
    }

    [Fact]
    public async Task RequestClaimEmailByIdentification_Throws_EmailMissing_When_Client_Has_No_Email()
    {
        // Arrange
        var requestEmail = Substitute.For<IRequestContactEmailVerification>();
        var clientNoEmail = new ClientEntity("Juan", email: null, identificationNumber: "12345678", phoneNumber: "3001112233", address: null);

        _unitOfWork.ClientsRepository.GetByIdentificationNumberAsync("12345678", Arg.Any<CancellationToken>())
            .Returns(clientNoEmail);

        var handler = new RequestClaimEmailByIdentificationHandler(_unitOfWork, requestEmail);

        var request = new RequestClaimEmailByIdentification("12345678");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.RequestAsync(request, CancellationToken.None));
        Assert.Equal(ClientErrorCodes.EmailMissing, ex.Code);
    }

    [Fact]
    public void Validator_Rejects_Client_Source_Mixing_And_Invalid_End_Time()
    {
        var validator = new QuickBookingAppointmentCommandValidator();
        var command = new QuickBookingAppointmentCommand(
            ClientId: Guid.NewGuid(),
            ClientPhoneNumber: "3001234567",
            ClientFullName: "Ana Cliente",
            PetName: "Fido",
            SpeciesId: Guid.NewGuid(),
            ServiceId: Guid.NewGuid(),
            VeterinarianId: Guid.NewGuid(),
            ScheduledStart: new DateTime(2026, 9, 25, 10, 0, 0),
            ScheduledEnd: new DateTime(2026, 9, 25, 9, 0, 0));

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.ClientPhoneNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.ClientFullName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.ScheduledEnd));
    }
}
