using System;
using System.Security;
using Engine.Api.Client.Registrations;
using Engine.Api.Client.Rooms;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using Engine.Network;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Registrations
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
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.UserDto == null)
        throw new ArgumentNullException(nameof(content.UserDto));

      var userId = content.UserDto.Id;
      if (userId == UserId.Empty)
        throw new ArgumentException(nameof(userId));

      if (userId.IsTemporary)
      {
        SendFail(args.ConnectionId, SystemMessageId.NotRegisteredBadName);
        return;
      }

      var userCertificate = ServerModel.Server.GetCertificate(args.ConnectionId);
      if (!string.Equals(userId.Thumbprint, userCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
      {
        SendFail(args.ConnectionId, SystemMessageId.NotRegisteredBadThumbprint);
        return;
      }
      
      using (var server = ServerModel.Get())
      {
        var chat = server.Chat;
        if (chat.IsNickExist(userId.Nick))
        {
          SendFail(args.ConnectionId, SystemMessageId.NotRegisteredNameAlreadyExist);
          return;
        }

        ServerModel.Logger.WriteInfo("User login: {0}", userId);

        chat.AddUser(new User(content.UserDto));

        var mainRoom = chat.GetRoom(ServerChat.MainRoomName);
        mainRoom.AddUser(userId);

        Register(userId, args.ConnectionId);

        var userDtos = chat.GetRoomUserDtos(mainRoom.Name);

        SendRefresh(userId, mainRoom, userDtos);
        SendOpened(userId, mainRoom, userDtos);

        // Notify
        ServerModel.Notifier.ConnectionRegistered(new ConnectionEventArgs(userId));
      }
    }

    private void Register(UserId userId, UserId tempUserId)
    {
      var messageContent = new ClientRegistrationResponseCommand.MessageContent { Registered = true };

      ServerModel.Server.RegisterConnection(tempUserId, userId);
      ServerModel.Server.SendMessage(userId, ClientRegistrationResponseCommand.CommandId, messageContent);
    }

    private void SendRefresh(UserId userId, Room room, UserDto[] users)
    {
      foreach (var currentId in room.Users)
      {
        if (currentId == userId)
          continue;

        var messageContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room.ToDto(currentId),
          Users = users
        };

        ServerModel.Server.SendMessage(currentId, ClientRoomRefreshedCommand.CommandId, messageContent);
      }
    }

    private void SendOpened(UserId userId, Room room, UserDto[] users)
    {
      var messageContent = new ClientRoomOpenedCommand.MessageContent
      {
        Room = room.ToDto(userId),
        Users = users
      };

      ServerModel.Server.SendMessage(userId, ClientRoomOpenedCommand.CommandId, messageContent);
    }

    private void SendFail(UserId userId, SystemMessageId message)
    {
      var regResponseContent = new ClientRegistrationResponseCommand.MessageContent { Registered = false, Message = message };
      ServerModel.Server.SendMessage(userId, ClientRegistrationResponseCommand.CommandId, regResponseContent, true);
      ServerModel.Api.Perform(new ServerRemoveUserAction(userId));
    }

    [Serializable]
    [BinType("ServerRegister")]
    public class MessageContent
    {
      [BinField("u")]
      public UserDto UserDto;
    }
  }
}
