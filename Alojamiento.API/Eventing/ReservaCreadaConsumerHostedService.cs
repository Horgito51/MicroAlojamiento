using System.Text;
using System.Text.Json;
using Alojamiento.Contracts.Events;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Entities.Eventing;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Reservas.Contracts.Events;

namespace Alojamiento.API.Eventing;

public sealed class ReservaCreadaConsumerHostedService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<ReservaCreadaConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private IModel? _channel;

    public ReservaCreadaConsumerHostedService(
        RabbitMqConnection connection,
        Microsoft.Extensions.Options.IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReservaCreadaConsumerHostedService> logger)
    {
        _connection = connection;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _channel = _connection.CreateChannel();
                _channel.BasicQos(0, 10, false);
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (_, args) => HandleMessage(args, stoppingToken);
                _channel.BasicConsume(_options.AlojamientoReservasQueue, autoAck: false, consumer);
                _logger.LogInformation("Alojamiento escuchando {Queue}", _options.AlojamientoReservasQueue);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible iniciar consumer RabbitMQ de Alojamiento. Reintentando en 10 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void HandleMessage(BasicDeliverEventArgs args, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AlojamientoDbContext>();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            var reservaCreada = JsonSerializer.Deserialize<ReservaCreadaIntegrationEvent>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (reservaCreada is null)
                throw new InvalidOperationException("No se pudo deserializar ReservaCreadaIntegrationEvent.");

            _logger.LogInformation(
                "Evento recibido en Alojamiento. RoutingKey={RoutingKey}, EventId={EventId}, CorrelationId={CorrelationId}",
                args.RoutingKey,
                reservaCreada.EventId,
                reservaCreada.CorrelationId);

            if (db.InboxMessages.Any(m => m.EventId == reservaCreada.EventId))
            {
                _channel?.BasicAck(args.DeliveryTag, false);
                return;
            }

            db.InboxMessages.Add(new InboxMessageEntity
            {
                EventId = reservaCreada.EventId,
                EventType = reservaCreada.EventType,
                EventVersion = reservaCreada.EventVersion,
                Source = reservaCreada.Source,
                CorrelationId = reservaCreada.CorrelationId
            });
            db.SaveChanges();

            var habitacionGuids = reservaCreada.Habitaciones
                .Select(h => h.HabitacionGuid)
                .Where(g => g.HasValue && g.Value != Guid.Empty)
                .Select(g => g!.Value)
                .Distinct()
                .ToList();

            if (habitacionGuids.Count > 0)
            {
                var disponibilidad = new DisponibilidadBloqueadaIntegrationEvent
                {
                    ReservaGuid = reservaCreada.ReservaGuid,
                    SucursalGuid = reservaCreada.SucursalGuid,
                    FechaInicio = reservaCreada.FechaInicio,
                    FechaFin = reservaCreada.FechaFin,
                    HabitacionGuids = habitacionGuids,
                    CorrelationId = reservaCreada.CorrelationId,
                    CausationId = reservaCreada.EventId
                };
                eventBus.PublishJsonAsync(
                    "alojamiento.disponibilidad.bloqueada.v1",
                    JsonSerializer.Serialize(disponibilidad, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    disponibilidad.EventId,
                    disponibilidad.CorrelationId,
                    disponibilidad.EventType,
                    ct).GetAwaiter().GetResult();
            }
            else
            {
                var noDisponible = new DisponibilidadNoDisponibleIntegrationEvent
                {
                    ReservaGuid = reservaCreada.ReservaGuid,
                    SucursalGuid = reservaCreada.SucursalGuid,
                    FechaInicio = reservaCreada.FechaInicio,
                    FechaFin = reservaCreada.FechaFin,
                    Motivo = "La reserva no contiene habitaciones asignadas para bloquear disponibilidad.",
                    CorrelationId = reservaCreada.CorrelationId,
                    CausationId = reservaCreada.EventId
                };
                eventBus.PublishJsonAsync(
                    "alojamiento.disponibilidad.no_disponible.v1",
                    JsonSerializer.Serialize(noDisponible, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    noDisponible.EventId,
                    noDisponible.CorrelationId,
                    noDisponible.EventType,
                    ct).GetAwaiter().GetResult();
            }

            var inbox = db.InboxMessages.First(m => m.EventId == reservaCreada.EventId);
            inbox.Status = "PRO";
            inbox.ProcessedOnUtc = DateTime.UtcNow;
            db.SaveChanges();
            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consumiendo ReservaCreada en Alojamiento. RoutingKey={RoutingKey}", args.RoutingKey);
            _channel?.BasicNack(args.DeliveryTag, false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
