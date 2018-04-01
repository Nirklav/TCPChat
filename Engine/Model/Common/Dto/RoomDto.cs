using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// Room data transfer object.
  /// </summary>
  [Serializable]
  [BinType("RoomDto")]
  public class RoomDto
  {
    [BinField("n")]
    public string Name;

    [BinField("a")]
    public UserId Admin;

    [BinField("u")]
    public UserId[] Users;

    [BinField("f")]
    public FileDescriptionDto[] Files;

    [BinField("m")]
    public MessageDto[] Messages;

    [BinField("t")]
    public RoomType Type;

    [BinField("c")]
    public UserId[] ConnectTo;

    public RoomDto(
      string name
      , UserId admin
      , IEnumerable<UserId> users
      , IEnumerable<FileDescription> files
      , IEnumerable<Message> messages
      , RoomType type
      , IEnumerable<UserId> connectTo)
    {
      Name = name;
      Admin = admin;
      Users = users.ToArray();
      Files = files.Select(f => f.ToDto()).ToArray();
      Messages = messages.Select(m => m.ToDto()).ToArray();

      Type = type;

      if (connectTo != null)
        ConnectTo = connectTo.ToArray();
    }
  }
}
