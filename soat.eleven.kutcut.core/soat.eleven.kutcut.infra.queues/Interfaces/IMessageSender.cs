namespace soat.eleven.kutcut.infra.queues.Interfaces
{
    public interface IMessageSender
    {
        Task SendMessage<Message>(string queueName, Message message);
    }
}
