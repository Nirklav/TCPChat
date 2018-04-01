using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Model.Server.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Rooms
{
  [SecurityCritical]
  class ClientRoomRefreshedCommand :
    ClientCommand<ClientRoomRefreshedCommand.MessageContent>
  {
    public const long CommandId = (long)ClientCommandId.RoomRefreshed;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (content.Room == null)
        throw new ArgumentNullException("content.Room");

      UpdateMessagesResult messagesResult;
      using (var client = ClientModel.Get())
      {
        var chat = client.Chat;
        var room = chat.GetRoom(content.Room.Name);

        AddUsers(chat, content.Users);
        UpdateRoomFiles(room, content.Room);
        messagesResult = UpdateRoomMessages(room, content.Room);
        var removedUsers = UpdateRoomUsers(room, content.Room);

        if (room.Name == ServerChat.MainRoomName)
          RemoveUsers(chat, removedUsers);
      }

      var roomArgs = new RoomRefreshedEventArgs(content.Room.Name, messagesResult.Added, messagesResult.Removed);
      ClientModel.Notifier.RoomRefreshed(roomArgs);
    }

    [SecurityCritical]
    private static HashSet<UserId> UpdateRoomUsers(Room room, RoomDto dto)
    {
      var removed = new HashSet<UserId>(room.Users);
      var added = new HashSet<UserId>(dto.Users);

      foreach (var nick in room.Users)
        added.Remove(nick);
      foreach (var nick in dto.Users)
        removed.Remove(nick);

      foreach (var nick in removed)
        room.RemoveUser(nick);

      foreach (var nick in added)
        room.AddUser(nick);

      return removed;
    }

    [SecurityCritical]
    private static void UpdateRoomFiles(Room room, RoomDto dto)
    {
      // Select removed and added files 
      var added = new HashSet<FileId>(dto.Files.Select(f => f.Id));
      foreach (var file in room.Files)
        added.Remove(file.Id);

      var removed = new HashSet<FileId>(room.Files.Select(f => f.Id));
      foreach (var file in dto.Files)
        removed.Remove(file.Id);

      // Add and remove files
      foreach (var fileId in removed)
        room.RemoveFile(fileId);

      foreach (var file in dto.Files)
      {
        if (!added.Contains(file.Id))
          continue;

        room.AddFile(new FileDescription(file));
      }
    }

    [SecurityCritical]
    private static UpdateMessagesResult UpdateRoomMessages(Room room, RoomDto dto)
    {
      // Select removed and added messages 
      var added = new HashSet<long>(dto.Messages.Select(m => m.Id));
      foreach (var message in room.Messages)
        added.Remove(message.Id);

      var removed = new HashSet<long>(room.Messages.Select(m => m.Id));
      foreach (var message in dto.Messages)
        removed.Remove(message.Id);

      // Add and remove messages
      foreach (var messageId in removed)
        room.RemoveMessage(messageId);

      foreach (var message in dto.Messages)
      {
        if (!added.Contains(message.Id))
          continue;

        room.AddMessage(new Message(message));
      }

      return new UpdateMessagesResult(added, removed);
    }

    private struct UpdateMessagesResult
    {
      public readonly HashSet<long> Added;
      public readonly HashSet<long> Removed;

      public UpdateMessagesResult(HashSet<long> added, HashSet<long> removed)
      {
        Added = added;
        Removed = removed;
      }
    }

    [Serializable]
    [BinType("ClientRoomRefreshed")]
    public class MessageContent
    {
      [BinField("r")]
      public RoomDto Room;

      [BinField("u")]
      public UserDto[] Users;
    }
  }
}
