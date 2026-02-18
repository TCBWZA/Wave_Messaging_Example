namespace Application.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ connection.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// The RabbitMQ server hostname or IP address.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// The RabbitMQ server port.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// The username for RabbitMQ authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// The password for RabbitMQ authentication.
    /// </summary>
    public string Password { get; set; } = "guest";
}
