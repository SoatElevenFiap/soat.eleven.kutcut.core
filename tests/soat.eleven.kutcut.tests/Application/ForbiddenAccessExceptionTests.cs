using FluentAssertions;
using soat.eleven.kutcut.application.Exceptions;

namespace soat.eleven.kutcut.tests.Application;

public class ForbiddenAccessExceptionTests
{
    [Fact]
    public void DefaultConstructor_UsesDefaultMessage()
    {
        var ex = new ForbiddenAccessException();

        ex.Message.Should().Contain("permissão");
    }

    [Fact]
    public void MessageConstructor_UsesProvidedMessage()
    {
        var ex = new ForbiddenAccessException("Acesso negado ao recurso.");

        ex.Message.Should().Be("Acesso negado ao recurso.");
    }

    [Fact]
    public void InnerExceptionConstructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ForbiddenAccessException("msg", inner);

        ex.Message.Should().Be("msg");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ForbiddenAccessException_IsAnException()
    {
        var ex = new ForbiddenAccessException();

        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ForbiddenAccessException_CanBeCaughtAsException()
    {
        Exception? caught = null;

        try
        {
            throw new ForbiddenAccessException("test");
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        caught.Should().NotBeNull();
        caught.Should().BeOfType<ForbiddenAccessException>();
    }
}
