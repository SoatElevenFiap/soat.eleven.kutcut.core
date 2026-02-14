namespace soat.eleven.kutcut.application.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Você não tem permissão para acessar este recurso.") { }

    public ForbiddenAccessException(string message)
        : base(message) { }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException) { }
}
