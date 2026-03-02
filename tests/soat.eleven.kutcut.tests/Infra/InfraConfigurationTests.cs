using FluentAssertions;
using soat.eleven.kutcut.infra.Configuration;

namespace soat.eleven.kutcut.tests.Infra;

public class InfraConfigurationTests
{
    // ── AuthServiceSettings ───────────────────────────────────────────────────

    [Fact]
    public void AuthServiceSettings_DefaultValues_AreCorrect()
    {
        var settings = new AuthServiceSettings();

        settings.BaseUrl.Should().BeEmpty();
        settings.TimeoutSeconds.Should().Be(0);
    }

    [Fact]
    public void AuthServiceSettings_SetProperties_RetainValues()
    {
        var settings = new AuthServiceSettings
        {
            BaseUrl = "http://auth-service",
            TimeoutSeconds = 30
        };

        settings.BaseUrl.Should().Be("http://auth-service");
        settings.TimeoutSeconds.Should().Be(30);
    }

    // ── AzureBlobStorageSettings ──────────────────────────────────────────────

    [Fact]
    public void AzureBlobStorageSettings_DefaultContainerName_IsKutcut()
    {
        var settings = new AzureBlobStorageSettings();

        settings.ContainerName.Should().Be("kutcut");
        settings.ConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void AzureBlobStorageSettings_SetProperties_RetainValues()
    {
        var settings = new AzureBlobStorageSettings
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "my-container"
        };

        settings.ConnectionString.Should().Be("UseDevelopmentStorage=true");
        settings.ContainerName.Should().Be("my-container");
    }

    // ── EmailSettings ─────────────────────────────────────────────────────────

    [Fact]
    public void EmailSettings_DefaultValues_AreCorrect()
    {
        var settings = new EmailSettings();

        settings.SmtpServer.Should().BeEmpty();
        settings.Port.Should().Be(0);
        settings.FromEmail.Should().BeEmpty();
        settings.FromName.Should().BeEmpty();
        settings.UserName.Should().BeEmpty();
        settings.Password.Should().BeEmpty();
        settings.UseSsl.Should().BeFalse();
    }

    [Fact]
    public void EmailSettings_SetProperties_RetainValues()
    {
        var settings = new EmailSettings
        {
            SmtpServer = "smtp.gmail.com",
            Port = 587,
            FromEmail = "test@test.com",
            FromName = "Test",
            UserName = "user",
            Password = "pass",
            UseSsl = true
        };

        settings.SmtpServer.Should().Be("smtp.gmail.com");
        settings.Port.Should().Be(587);
        settings.FromEmail.Should().Be("test@test.com");
        settings.FromName.Should().Be("Test");
        settings.UserName.Should().Be("user");
        settings.Password.Should().Be("pass");
        settings.UseSsl.Should().BeTrue();
    }

    // ── RabbitMQSettings ──────────────────────────────────────────────────────

    [Fact]
    public void RabbitMQSettings_DefaultValues_AreCorrect()
    {
        var settings = new RabbitMQSettings();

        settings.HostName.Should().BeEmpty();
        settings.Port.Should().Be(0);
        settings.UserName.Should().BeEmpty();
        settings.Password.Should().BeEmpty();
        settings.VideoProcessingQueueName.Should().BeEmpty();
        settings.VideoUploadedQueueName.Should().BeEmpty();
    }

    [Fact]
    public void RabbitMQSettings_SetProperties_RetainValues()
    {
        var settings = new RabbitMQSettings
        {
            HostName = "rabbitmq",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VideoProcessingQueueName = "processing_queue",
            VideoUploadedQueueName = "uploaded_queue"
        };

        settings.HostName.Should().Be("rabbitmq");
        settings.Port.Should().Be(5672);
        settings.UserName.Should().Be("guest");
        settings.Password.Should().Be("guest");
        settings.VideoProcessingQueueName.Should().Be("processing_queue");
        settings.VideoUploadedQueueName.Should().Be("uploaded_queue");
    }
}
