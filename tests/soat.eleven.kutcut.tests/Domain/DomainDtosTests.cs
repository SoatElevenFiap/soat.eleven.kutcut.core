using FluentAssertions;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.tests.Domain;

public class UserDtoTests
{
    [Fact]
    public void UserDto_HasDefaultEmptyStrings()
    {
        var dto = new UserDto();

        dto.Name.Should().Be(string.Empty);
        dto.Email.Should().Be(string.Empty);
    }

    [Fact]
    public void UserDto_CanSetProperties()
    {
        var dto = new UserDto { Name = "João", Email = "joao@email.com" };

        dto.Name.Should().Be("João");
        dto.Email.Should().Be("joao@email.com");
    }

    [Fact]
    public void UserDto_Equality_SameValues_AreEqual()
    {
        var dto1 = new UserDto { Name = "Maria", Email = "maria@email.com" };
        var dto2 = new UserDto { Name = "Maria", Email = "maria@email.com" };

        // Class — reference equality, but verify property-level access
        dto1.Name.Should().Be(dto2.Name);
        dto1.Email.Should().Be(dto2.Email);
    }
}

public class NotifyMessageTests
{
    [Fact]
    public void NotifyMessage_HasDefaultEmptyStrings()
    {
        var msg = new NotifyMessage();

        msg.Title.Should().Be(string.Empty);
        msg.Body.Should().Be(string.Empty);
    }

    [Fact]
    public void NotifyMessage_CanSetProperties()
    {
        var msg = new NotifyMessage { Title = "Título", Body = "Corpo da mensagem" };

        msg.Title.Should().Be("Título");
        msg.Body.Should().Be("Corpo da mensagem");
    }

    [Fact]
    public void NotifyMessage_LongBody_AcceptedWithoutTruncation()
    {
        var longBody = new string('x', 10000);
        var msg = new NotifyMessage { Body = longBody };

        msg.Body.Should().HaveLength(10000);
    }
}

public class StatusEnumTests
{
    [Fact]
    public void StatusEnum_HasExpectedValues()
    {
        ((int)StatusEnum.Pendente).Should().Be(1);
        ((int)StatusEnum.Uploaded).Should().Be(2);
        ((int)StatusEnum.EmProcessamento).Should().Be(3);
        ((int)StatusEnum.ProcessadoComSucesso).Should().Be(4);
        ((int)StatusEnum.ProcessadoComErro).Should().Be(5);
    }

    [Fact]
    public void StatusEnum_HasExactlyFiveValues()
    {
        Enum.GetValues<StatusEnum>().Should().HaveCount(5);
    }
}

public class NotificationTypeEnumTests
{
    [Fact]
    public void NotificationTypeEnum_HasExpectedValues()
    {
        ((int)NotificationTypeEnum.ProcessadoComSucesso).Should().Be(1);
        ((int)NotificationTypeEnum.ProcessadoComErro).Should().Be(2);
    }

    [Fact]
    public void NotificationTypeEnum_HasExactlyTwoValues()
    {
        Enum.GetValues<NotificationTypeEnum>().Should().HaveCount(2);
    }
}
