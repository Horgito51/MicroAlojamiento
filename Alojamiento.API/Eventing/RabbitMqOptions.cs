namespace Alojamiento.API.Eventing;

public sealed class RabbitMqOptions
{
    public string? Uri { get; set; }
    public bool UseSsl { get; set; }
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "hotel.integration.events";
    public string DeadLetterExchangeName { get; set; } = "hotel.integration.dlx";
    public string AlojamientoReservasQueue { get; set; } = "alojamiento.reservas.queue";
    public string AlojamientoReservasDlq { get; set; } = "alojamiento.reservas.dlq";
}
