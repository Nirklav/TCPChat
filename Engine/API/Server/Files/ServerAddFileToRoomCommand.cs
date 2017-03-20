using System;
using System.Security;
using Engine.Api.Client.Files;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Files
{
  [SecurityCritical]
  class ServerAddFileToRoomCommand :
    ServerCommand<ServerAddFileToRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.AddFileToRoom;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      if (content.File == null)
        throw new ArgumentNullException("content.File");

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.IsUserExist(args.ConnectionId))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        if (!room.IsFileExist(content.File.Id))
          room.AddFile(new FileDescription(content.File));

        var sendingContent = new ClientFilePostedCommand.MessageContent
        {
          File = content.File,
          RoomName = content.RoomName
        };

        foreach (var user in room.Users)
          ServerModel.Server.SendMessage(user, ClientFilePostedCommand.CommandId, sendingContent);
      }
    }

    [Serializable]
    [BinType("ServerAddFileToRoom")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;

      [BinField("f")]
      public FileDescriptionDto File;
    }
  }
}
