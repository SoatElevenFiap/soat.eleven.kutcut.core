using soat.eleven.kutcut.domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace soat.eleven.kutcut.domain.Notifications
{
    public interface IUserNotificaton
    {
        bool NotifyUser(UserDto user, NotifyMessage message);
    }
}
