using System.Text.Json.Serialization;

namespace soat.eleven.kutcut.infra.queues.MessagesDtos
{
    public class VideoUploadedMessage
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("messageId")]
        public Guid MessageId { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
