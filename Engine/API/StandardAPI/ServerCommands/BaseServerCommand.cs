using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;

namespace Engine.API.StandardAPI.ServerCommands
{
  abstract class BaseServerCommand : BaseCommand
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
  }
}
