using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// Room data transfer object.
  /// </summary>
  [Serializable]
  public class RoomDto
  {
    public readonly string Name;
    public readonly string Admin;
    public readonly List<string> Users;
    public readonly List<FileDescription> Files;
    public readonly List<Message> Messages;

    public readonly RoomType Type;
    public readonly List<string> ConnectTo;

    public RoomDto(
      string name
      , string admin
      , IEnumerable<string> users
      , IEnumerable<FileDescription> files
      , Dictionary<long, Message> messages
      , RoomType type
      , List<string> connectTo)
    {
      Name = name;
      Admin = admin;
      Users = new List<string>(users);
      Files = new List<FileDescription>(files);
      Messages = messages.Select(kvp => kvp.Value.Clone()).ToList();

      Type = type;
      ConnectTo = new List<string>(connectTo);
    }
  }
}
