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
    public readonly Dictionary<string, List<string>> ConnectionsMap;

    public RoomDto(
      string name
      , string admin
      , IEnumerable<string> users
      , IEnumerable<FileDescription> files
      , Dictionary<long, Message> messages
      , RoomType type
      , Dictionary<string, List<string>> connectionsMap)
    {
      Name = name;
      Admin = admin;
      Users = new List<string>(users);
      Files = new List<FileDescription>(files);
      Messages = messages.Select(kvp => kvp.Value.Clone()).ToList();
      Type = type;

      ConnectionsMap = connectionsMap != null 
        ? connectionsMap.ToDictionary(kvp => kvp.Key, kvp => new List<string>(kvp.Value))
        : null;
    }
  }
}
