using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System.Linq;

namespace Engine.API.ServerCommands
{
  abstract class BaseServerCommand
  {
    /// <summary>
    /// Проверяет существует ли комната. Если нет отправляет вызвавщему команду соединению сообщение об ошибке. 
    /// А также команду закрытия комнаты.
    /// </summary>
    /// <param name="RoomName">Название комнаты.</param>
    /// <param name="connectionId">Id соединения.</param>
    /// <returns>Возвращает ложь если комнаты не существует.</returns>
    protected static bool RoomExists(string roomName, string connectionId)
    {
      bool result;
      using(var context = ServerModel.Get())
        result = context.Rooms.ContainsKey(roomName);

      if (!result)
      {
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = new Room(null, roomName) };
        ServerModel.Server.SendMessage(connectionId, ClientRoomClosedCommand.Id, closeRoomContent);
        ServerModel.API.SendSystemMessage(connectionId, "На свервере нет комнаты с таким именем.");
      }

      return result;
    }

    /// <summary>
    /// Посылает команду обновления комнаты всем ее участникам.
    /// </summary>
    /// <param name="server">Контекст сервера.</param>
    /// <param name="room">Комната.</param>
    protected static void RefreshRoom(ServerContext server, Room room)
    {
      var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
      {
        Room = room,
        Users = room.Users.Select(nick => server.Users[nick]).ToList()
      };

      foreach (string user in room.Users)
        ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.Id, roomRefreshedContent);
    }
  }
}
