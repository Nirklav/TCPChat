using Engine.Api.Client.Rooms;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Server.Registrations
{
  [Serializable]
  public class ServerRemoveUserAction : IAction
  {
    private readonly UserId _userId;
    private readonly bool _removeConnection;

    /// <summary>
    /// Removes user form chat and close all him resources.
    /// </summary>
    /// <param name="userId">User nick who be removed.</param>
    /// <param name="removeConnection">Can be server connection removed or not.</param>
    public ServerRemoveUserAction(UserId userId, bool removeConnection = true)
    {
      _userId = userId;
      _removeConnection = removeConnection;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      using (var server = ServerModel.Get())
      {
        var emptyRooms = new List<string>();
        foreach (var room in server.Chat.GetRooms())
        {
          if (!room.Users.Contains(_userId))
            continue;

          room.RemoveUser(_userId);

          if (room.Name != ServerChat.MainRoomName)
          {
            if (room.IsEmpty)
              emptyRooms.Add(room.Name);
            else
            {
              if (room.Admin == _userId)
              {
                room.Admin = room.Users.FirstOrDefault();
                if (room.Admin != UserId.Empty)
                  ServerModel.Api.Perform(new ServerSendSystemMessageAction(room.Admin, SystemMessageId.RoomAdminChanged, room.Name));
              }
            }
          }

          foreach (var userNick in room.Users)
          {
            var sendingContent = new ClientRoomRefreshedCommand.MessageContent
            {
              Room = room.ToDto(userNick),
              Users = server.Chat.GetRoomUserDtos(room.Name)
            };
            ServerModel.Server.SendMessage(userNick, ClientRoomRefreshedCommand.CommandId, sendingContent);
          }
        }

        // Remove all empty rooms
        foreach (var emptyRoomName in emptyRooms)
          server.Chat.RemoveRoom(emptyRoomName);

        // Removing user from chat after all rooms processing
        server.Chat.RemoveUser(_userId);
      }

      ServerModel.Notifier.ConnectionUnregistered(new ConnectionEventArgs(_userId));

      // Closing the connection after model clearing
      if (_removeConnection)
        ServerModel.Server.CloseConnection(_userId);
    }
  }
}
