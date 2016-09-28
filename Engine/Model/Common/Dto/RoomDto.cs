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
    public string Admin;

    [BinField("u")]
    public string[] Users;

    [BinField("f")]
    public FileDescriptionDto[] Files;

    [BinField("m")]
    public MessageDto[] Messages;

    [BinField("t")]
    public RoomType Type;

    [BinField("c")]
    public string[] ConnectTo;

    public RoomDto(
      string name
      , string admin
      , IEnumerable<string> users
      , IEnumerable<FileDescription> files
      , IEnumerable<Message> messages
      , RoomType type
      , IEnumerable<string> connectTo)
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
