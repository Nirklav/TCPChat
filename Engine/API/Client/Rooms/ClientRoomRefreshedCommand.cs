using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Client.Entities;
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

      using (var client = ClientModel.Get())
      {
        var chat = client.Chat;
        var room = chat.GetRoom(content.Room.Name);

        AddUsers(chat, content.Users);
        UpdateRoomFiles(chat, room, content.Room);
        var removedUsers = UpdateRoomUsers(chat, room, content.Room);

        if (room.Name == ServerChat.MainRoomName)
        {
          foreach (var nick in removedUsers)
            chat.RemoveUser(nick);
        }
      }

      var users = content.Users
        .Select(u => u.Nick)
        .ToList();

      ClientModel.Notifier.RoomRefreshed(new RoomEventArgs(content.Room.Name, users));
    }

    [SecurityCritical]
    private static HashSet<string> UpdateRoomUsers(ClientChat chat, Room room, RoomDto dto)
    {
      var removed = new HashSet<string>(room.Users);
      var added = new HashSet<string>(dto.Users);

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
    private static void UpdateRoomFiles(ClientChat chat, Room room, RoomDto dto)
    {
      // Select removed and added files
      var removed = new HashSet<FileId>(room.Files.Select(f => f.Id));
      var added = new HashSet<FileId>(dto.Files.Select(f => f.Id));

      foreach (var file in room.Files)
        added.Remove(file.Id);
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
