using Engine.Api.Client;
using Engine.Api.Client.Rooms;
using Engine.Api.Server.Messages;
using Engine.Exceptions;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using Engine.Network;
using Engine.Plugins;
using System.Security;

namespace Engine.Api.Server
{
  public abstract class ServerCommand :
    CrossDomainObject,
    ICommand
  {
    public abstract long Id
    {
      [SecuritySafeCritical]
      get;
    }

    [SecuritySafeCritical]
    public void Run(CommandArgs args)
    {
      if (args.ConnectionId == null)
        throw new ModelException(ErrorCode.IllegalInvoker, string.Format("For the server command ConnectionId is required {0}", GetType().FullName));

      OnRun(args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(CommandArgs args);
  }

  public abstract class ServerCommand<TContent> : ServerCommand
  {
    [SecuritySafeCritical]
    protected sealed override void OnRun(CommandArgs args)
    {
      var package = args.Unpacked.Package as IPackage<TContent>;
      if (package == null)
        throw new ModelException(ErrorCode.WrongContentType);

      OnRun(package.Content, args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(TContent content, CommandArgs args);

    #region Helpers
    /// <summary>
    /// Trying to get room. If room not found then it send error message and close room command.
    /// </summary>
    /// <param name="chat">Server chat instance.</param>
    /// <param name="roomName">Room name.</param>
    /// <param name="userId">User id.</param>
    /// <param name="room">Result room.</param>
    /// <returns>Returns true if room found, otherwise false.</returns>
    [SecurityCritical]
    protected static bool TryGetRoom(ServerChat chat, string roomName, UserId userId, out Room room)
    {
      room = chat.TryGetRoom(roomName);
      if (room == null)
      {
        var closeRoomContent = new ClientRoomClosedCommand.MessageContent { RoomName = roomName };
        ServerModel.Server.SendMessage(userId, ClientRoomClosedCommand.CommandId, closeRoomContent);
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(userId, SystemMessageId.RoomNotExist));
      }
      return room != null;
    }

    /// <summary>
    /// Send refresh commands to all room users.
    /// </summary>
    /// <param name="server">Server chat.</param>
    /// <param name="room">Refreshed room.</param>
    [SecurityCritical]
    protected static void RefreshRoom(ServerChat chat, Room room)
    {
      var users = chat.GetRoomUserDtos(room.Name);

      foreach (var userNick in room.Users)
      {
        var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room.ToDto(userNick),
          Users = users
        };

        ServerModel.Server.SendMessage(userNick, ClientRoomRefreshedCommand.CommandId, roomRefreshedContent);
      }
    }
    #endregion
  }
}
