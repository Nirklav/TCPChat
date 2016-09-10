using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Model.Common.Entities
{
  /// <summary>
  /// Класс описывающий голосовую комнату.
  /// </summary>
  [Serializable]
  public class VoiceRoom : Room
  {
    private readonly Dictionary<string, List<string>> _connectionMap;

    /// <summary>
    /// Создает голосовую комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    public VoiceRoom(string admin, string name)
      : base(admin, name) 
    {
      _connectionMap = new Dictionary<string, List<string>>();
      _connectionMap.Add(admin, new List<string>());
    }

    /// <summary>
    /// Создает голосовую комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    /// <param name="initialUsers">Начальный список пользователей комнаты. Уже существуюшие пользователе повторно добавлены не будут.</param>
    public VoiceRoom(string admin, string name, IEnumerable<User> initialUsers)
      : base(admin, name, initialUsers) 
    {
      _connectionMap = new Dictionary<string, List<string>>();

      for (int i = 0; i < _users.Count; i++)
      {
        var connections = new List<string>();
        for (int m = i + 1; m < _users.Count; m++)
          connections.Add(_users[m]);

        _connectionMap.Add(_users[i], connections);
      }
    }

    /// <summary>
    /// Тип комнаты.
    /// </summary>
    public override RoomType Type
    {
      get { return RoomType.Voice; }
    }

    /// <summary>
    /// Добавляет пользователя в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public override void AddUser(string nick)
    {
      base.AddUser(nick);

      var users = _connectionMap.Keys.ToList();

      foreach (var kvp in _connectionMap)
        kvp.Value.Add(nick);

      _connectionMap.Add(nick, users);
    }

    /// <summary>
    /// Удаляет пользователя из комнаты.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public override void RemoveUser(string nick)
    {
      base.RemoveUser(nick);

      foreach (var kvp in _connectionMap)
        kvp.Value.Remove(nick);

      _connectionMap.Remove(nick);
    }

    /// <summary>
    /// Карта соединений. 
    /// Кey - пользователь который должен инциировать соединения всем кто находится в списке (Value).
    /// </summary>
    public Dictionary<string, List<string>> ConnectionMap
    {
      get { return _connectionMap; }
    }
  }
}
