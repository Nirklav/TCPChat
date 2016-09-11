using Engine.Api.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Engine.Api.Server
{
  [SecurityCritical]
  class ServerRegisterCommand :
    ServerCommand<ServerRegisterCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.Register;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (content.User == null)
        throw new ArgumentNullException("User");

      if (content.User.Nick == null)
        throw new ArgumentNullException("User.Nick");

      if (content.User.Nick.Contains(Connection.TempConnectionPrefix))
      {
        SendFail(args.ConnectionId, SystemMessageId.NotRegisteredBadName);
        return;
      }
      
      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[ServerModel.MainRoomName];    
        var userExist = room.Users.Any(nick => string.Equals(content.User.Nick, nick));

        if (userExist)
        {
          SendFail(args.ConnectionId, SystemMessageId.NotRegisteredNameAlreadyExist);
          return;
        }
        else
        {
          ServerModel.Logger.WriteInfo("User login: {0}", content.User.Nick);

          server.Users.Add(content.User.Nick, content.User);
          room.AddUser(content.User.Nick);

          Register(content.User.Nick, args.ConnectionId);

          var users = ServerModel.Api.GetRoomUsers(server, room);
          SendRefresh(content.User.Nick, room, users);
          SendOpened(content.User.Nick, room, users);

          // Notify
          ServerModel.Notifier.Registered(new ServerRegistrationEventArgs { Nick = content.User.Nick });
        }
      }
    }

    private void Register(string userNick, string tempId)
    {
      var messageContent = new ClientRegistrationResponseCommand.MessageContent { Registered = true };

      ServerModel.Server.RegisterConnection(tempId, userNick);
      ServerModel.Server.SendMessage(userNick, ClientRegistrationResponseCommand.CommandId, messageContent);
    }

    private void SendRefresh(string userNick, Room room, List<User> users)
    {
      var messageContent = new ClientRoomRefreshedCommand.MessageContent
      {
        Room = room,
        Users = users
      };

      foreach (var nick in room.Users)
      {
        if (nick == userNick)
          continue;

        ServerModel.Server.SendMessage(nick, ClientRoomRefreshedCommand.CommandId, messageContent);
      }
    }

    private void SendOpened(string userNick, Room room, List<User> users)
    {
      var messageContent = new ClientRoomOpenedCommand.MessageContent
      {
        Room = room,
        Users = users,
        Type = room.Type
      };

      ServerModel.Server.SendMessage(userNick, ClientRoomOpenedCommand.CommandId, messageContent);
    }

    private void SendFail(string connectionId, SystemMessageId message)
    {
      var regResponseContent = new ClientRegistrationResponseCommand.MessageContent { Registered = false, Message = message };
      ServerModel.Server.SendMessage(connectionId, ClientRegistrationResponseCommand.CommandId, regResponseContent, true);
      ServerModel.Api.RemoveUser(connectionId);
    }

    [Serializable]
    public class MessageContent
    {
      private UserDto _user;

      public UserDto User
      {
        get { return _user; }
        set { _user = value; }
      }
    }
  }
}
