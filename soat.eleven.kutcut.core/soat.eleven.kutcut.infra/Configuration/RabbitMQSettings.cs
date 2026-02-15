namespace soat.eleven.kutcut.infra.Configuration
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string VideoProcessingQueueName { get; set; } = string.Empty;
        public string VideoUploadedQueueName { get; set; } = string.Empty;
    }
}
