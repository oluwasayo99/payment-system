namespace Demo.Common.Settings;

public class RabbitMqSettings
{
    public string Host { get; init; }
    public int Port { get; init; } = 5672;


    public string ConnectionUri =>
        $"rabbitmq://{Host}:{Port}";

}