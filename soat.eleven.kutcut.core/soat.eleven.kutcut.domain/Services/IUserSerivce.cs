using soat.eleven.kutcut.domain.Dtos;

namespace soat.eleven.kutcut.domain.Services
{
    public interface IUserSerivce
    {
        UserDto GetUser(Guid userId);
    }
}
