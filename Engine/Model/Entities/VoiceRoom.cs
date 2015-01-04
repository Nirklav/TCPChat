using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий голосовую комнату.
  /// </summary>
  [Serializable]
  public class VoiceRoom : Room
  {
    [NonSerialized]
    private bool enabled;

    private Dictionary<string, List<string>> connectionMap;

    /// <summary>
    /// Создает голосовую комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    public VoiceRoom(string admin, string name)
      : base(admin, name) 
    {
      connectionMap = new Dictionary<string, List<string>>();
      connectionMap.Add(admin, new List<string>());
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
      connectionMap = new Dictionary<string, List<string>>();

      var userNames = users.Keys.ToList();
      for (int i = 0; i < userNames.Count; i++)
      {
        var connections = new List<string>();
        for (int m = i + 1; m < userNames.Count; m++)
          connections.Add(userNames[m]);

        connectionMap.Add(userNames[i], connections);
      }
    }

    /// <summary>
    /// Добавляет пользователя в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public override void AddUser(string nick)
    {
      base.AddUser(nick);

      var users = connectionMap.Keys.ToList();

      foreach (var kvp in connectionMap)
        kvp.Value.Add(nick);

      connectionMap.Add(nick, users);
    }

    /// <summary>
    /// Удаляет пользователя из комнаты.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public override void RemoveUser(string nick)
    {
      base.RemoveUser(nick);

      foreach (var kvp in connectionMap)
        kvp.Value.Remove(nick);

      connectionMap.Remove(nick);
    }

    /// <summary>
    /// Включено ли вещание для этой комнаты.
    /// </summary>
    public bool Enabled
    {
      get { return enabled; }
      set { enabled = value; }
    }

    /// <summary>
    /// Карта соединений. 
    /// Кey - пользователь который должен инциировать соединения всем кто находится в списке (Value).
    /// </summary>
    public Dictionary<string, List<string>> ConnectionMap
    {
      get { return connectionMap; }
    }
  }
}
