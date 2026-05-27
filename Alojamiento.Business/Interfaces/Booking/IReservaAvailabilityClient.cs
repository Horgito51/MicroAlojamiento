namespace Alojamiento.Business.Interfaces.Booking
{
    public interface IReservaAvailabilityClient
    {
        Task<IReadOnlySet<int>> GetHabitacionesReservadasAsync(
            int idSucursal,
            DateTime fechaInicio,
            DateTime fechaFin,
            CancellationToken ct = default);
    }
}
