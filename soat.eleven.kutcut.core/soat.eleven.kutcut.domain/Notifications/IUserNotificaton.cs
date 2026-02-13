using soat.eleven.kutcut.domain.Dtos;

namespace soat.eleven.kutcut.domain.Notifications
{
    public interface IUserNotificaton
    {
        bool NotifyUser(UserDto user, NotifyMessage message);
    }
}
