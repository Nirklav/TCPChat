using Engine.Api.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using Engine.Network;
using System;
using System.Collections.Generic;
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
      if (content.UserDto == null)
        throw new ArgumentNullException("User");

      if (content.UserDto.Nick == null)
        throw new ArgumentNullException("User.Nick");

      if (content.UserDto.Nick.Contains(Connection.TempConnectionPrefix))
      {
        SendFail(args.ConnectionId, SystemMessageId.NotRegisteredBadName);
        return;
      }
      
      using (var server = ServerModel.Get())
      {
        var chat = server.Chat;
        if (chat.IsUserExist(content.UserDto.Nick))
        {
          SendFail(args.ConnectionId, SystemMessageId.NotRegisteredNameAlreadyExist);
          return;
        }
        else
        {
          ServerModel.Logger.WriteInfo("User login: {0}", content.UserDto.Nick);

          chat.AddUser(new User(content.UserDto));

          var mainRoom = chat.GetRoom(ServerChat.MainRoomName);
          mainRoom.AddUser(content.UserDto.Nick);

          Register(content.UserDto.Nick, args.ConnectionId);

          var userDtos = chat.GetRoomUserDtos(mainRoom.Name);

          SendRefresh(content.UserDto.Nick, mainRoom, userDtos);
          SendOpened(content.UserDto.Nick, mainRoom, userDtos);

          // Notify
          ServerModel.Notifier.Registered(new ServerRegistrationEventArgs { Nick = content.UserDto.Nick });
        }
      }
    }

    private void Register(string userNick, string tempId)
    {
      var messageContent = new ClientRegistrationResponseCommand.MessageContent { Registered = true };

      ServerModel.Server.RegisterConnection(tempId, userNick);
      ServerModel.Server.SendMessage(userNick, ClientRegistrationResponseCommand.CommandId, messageContent);
    }

    private void SendRefresh(string userNick, Room room, List<UserDto> users)
    {
      foreach (var nick in room.Users)
      {
        if (nick == userNick)
          continue;

        var messageContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room.ToDto(nick),
          Users = users
        };

        ServerModel.Server.SendMessage(nick, ClientRoomRefreshedCommand.CommandId, messageContent);
      }
    }

    private void SendOpened(string userNick, Room room, List<UserDto> users)
    {
      var messageContent = new ClientRoomOpenedCommand.MessageContent
      {
        Room = room.ToDto(userNick),
        Users = users
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

      public UserDto UserDto
      {
        get { return _user; }
        set { _user = value; }
      }
    }
  }
}
