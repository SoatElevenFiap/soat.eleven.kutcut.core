namespace soat.eleven.kutcut.infra.queues.Interfaces
{
    public interface IMessageListener
    {
        void StartListening(string queueName, Func<string, Task> onMessageReceived);
        void StopListening();
    }
}
