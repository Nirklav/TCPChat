using Engine.Api.Client;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Server.Registrations
{
  [Serializable]
  public class ServerRemoveUserAction : IAction
  {
    private string _nick;
    private bool _removeConnection;

    /// <summary>
    /// Removes user form chat and close all him resources.
    /// </summary>
    /// <param name="nick">User nick who be removed.</param>
    /// <param name="removeConnection">Can be server connection removed or not.</param>
    public ServerRemoveUserAction(string nick, bool removeConnection = true)
    {
      _nick = nick;
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
          if (!room.Users.Contains(_nick))
            continue;

          room.RemoveUser(_nick);
          if (room.IsEmpty)
            emptyRooms.Add(room.Name);
          else
          {
            if (room.Admin == _nick)
            {
              room.Admin = room.Users.FirstOrDefault();
              if (room.Admin != null)
                ServerModel.Api.Perform(new ServerSendSystemMessageAction(room.Admin, SystemMessageId.RoomAdminChanged, room.Name));
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
        }

        // Remove all empty rooms
        foreach (var emptyRoomName in emptyRooms)
          server.Chat.RemoveRoom(emptyRoomName);

        // Removing user from chat after all rooms processing
        server.Chat.RemoveUser(_nick);
      }

      ServerModel.Notifier.ConnectionUnregistered(new ConnectionEventArgs(_nick));

      // Closing the connection after model clearing
      if (_removeConnection)
        ServerModel.Server.CloseConnection(_nick);
    }
  }
}
