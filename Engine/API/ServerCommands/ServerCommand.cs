using Engine.API.ClientCommands;
using Engine.Exceptions;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using Engine.Plugins;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  public abstract class ServerCommand :
    CrossDomainObject,
    ICommand<ServerCommandArgs>
  {
    public abstract long Id
    {
      [SecuritySafeCritical]
      get;
    }

    [SecuritySafeCritical]
    public void Run(ServerCommandArgs args)
    {
      if (args.ConnectionId == null)
        throw new ModelException(ErrorCode.IllegalInvoker, string.Format("For the server command ConnectionId is required {0}", GetType().FullName));

      OnRun(args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(ServerCommandArgs args);
  }

  public abstract class ServerCommand<TContent> : ServerCommand
  {
    [SecuritySafeCritical]
    protected sealed override void OnRun(ServerCommandArgs args)
    {
      var package = args.Package as IPackage<TContent>;
      if (package == null)
        throw new ModelException(ErrorCode.WrongContentType);

      OnRun(package.Content, args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(TContent content, ServerCommandArgs args);

    #region Helpers
    /// <summary>
    /// Проверяет существует ли комната. Если нет отправляет вызвавщему команду соединению сообщение об ошибке. 
    /// А также команду закрытия комнаты.
    /// </summary>
    /// <param name="RoomName">Название комнаты.</param>
    /// <param name="connectionId">Id соединения.</param>
    /// <returns>Возвращает false если комнаты не существует.</returns>
    [SecurityCritical]
    protected static bool RoomExists(string roomName, string connectionId)
    {
      bool result;
      using(var context = ServerModel.Get())
        result = context.Rooms.ContainsKey(roomName);

      if (!result)
      {
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = new Room(null, roomName) };
        ServerModel.Server.SendMessage(connectionId, ClientRoomClosedCommand.CommandId, closeRoomContent);
        ServerModel.Api.SendSystemMessage(connectionId, MessageId.RoomNotExist);
      }

      return result;
    }

    /// <summary>
    /// Посылает команду обновления комнаты всем ее участникам.
    /// </summary>
    /// <param name="server">Контекст сервера.</param>
    /// <param name="room">Комната.</param>
    [SecurityCritical]
    protected static void RefreshRoom(ServerContext server, Room room)
    {
      var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
      {
        Room = room,
        Users = room.Users.Select(nick => server.Users[nick]).ToList()
      };

      foreach (string user in room.Users)
        ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.CommandId, roomRefreshedContent);
    }
    #endregion
  }
}
