using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using System;
using System.Security;

namespace Engine.Model.Client.Entities
{
  [Serializable]
  public class ClientRoom : Room
  {
    [SecuritySafeCritical]
    public ClientRoom(RoomDto dto)
      : base(dto.Admin, dto.Name)
    {
      foreach (var nick in dto.Users)
        _users.Add(nick);

      foreach (var file in dto.Files)
        _files.Add(file.Id, file);

      foreach (var message in dto.Messages)
        _messages.Add(message.Id, message);
    }
  }
}
