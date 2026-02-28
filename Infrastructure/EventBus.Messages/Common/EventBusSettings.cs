namespace EventBus.Messages.Common;

public class EventBusSettings
{
    public string HostAddress { get; set; } = "amqp://guest:guest@localhost:5672";
}
