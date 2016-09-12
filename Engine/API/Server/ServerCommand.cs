using Engine.Api.Client;
using Engine.Exceptions;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Network;
using Engine.Plugins;
using System.Security;

namespace Engine.Api.Server
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
    /// Trying to get room. If room not found then it send error message and close room command.
    /// </summary>
    /// <param name="server">Server guard instance.</param>
    /// <param name="roomName">Room name.</param>
    /// <param name="connectionId">Connection id.</param>
    /// <param name="room">Result room.</param>
    /// <returns>Returns true if room found, otherwise false..</returns>
    [SecurityCritical]
    protected static bool TryGetRoom(ServerGuard server, string roomName, string connectionId, out Room room)
    {
      room = server.Chat.TryGetRoom(roomName);
      if (room == null)
      {
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { RoomName = roomName };
        ServerModel.Server.SendMessage(connectionId, ClientRoomClosedCommand.CommandId, closeRoomContent);
        ServerModel.Api.SendSystemMessage(connectionId, SystemMessageId.RoomNotExist);
      }
      return room != null;
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
