using Engine.Api.ClientCommands;
using Engine.Exceptions;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network;
using Engine.Plugins;
using System.Security;

namespace Engine.Api.ServerCommands
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
      var package = args.Unpacked.Package as IPackage<TContent>;
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
    protected static bool TryGetRoom(ServerGuard server, string roomName, string connectionId, out Room room)
    {
      var result = server.Rooms.TryGetValue(roomName, out room);
      if (!result)
      {
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { Room = new Room(null, roomName) };
        ServerModel.Server.SendMessage(connectionId, ClientRoomClosedCommand.CommandId, closeRoomContent);
        ServerModel.Api.SendSystemMessage(connectionId, SystemMessageId.RoomNotExist);
      }
      return result;
    }

    /// <summary>
    /// Посылает команду обновления комнаты всем ее участникам.
    /// </summary>
    /// <param name="server">Контекст сервера.</param>
    /// <param name="room">Комната.</param>
    [SecurityCritical]
    protected static void RefreshRoom(ServerGuard server, Room room)
    {
      var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
      {
        Room = room,
        Users = ServerModel.Api.GetRoomUsers(server, room)
      };

      foreach (var user in room.Users)
        ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.CommandId, roomRefreshedContent);
    }
    #endregion
  }
}
