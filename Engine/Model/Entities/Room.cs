using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий комнату.
  /// </summary>
  [Serializable]
  public class Room
  {
    /// <summary>
    /// Идентификатор сообщений которые невозможно редактировать.
    /// </summary>
    public const long SpecificMessageId = -1;

    protected readonly string name;
    protected readonly Dictionary<string, RoomUser> users;
    protected readonly Dictionary<long, Message> messages;
    protected readonly List<FileDescription> files;

    protected string admin;
    protected long lastMessageId;

    /// <summary>
    /// Создает комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    public Room(string admin, string name)
    {
      this.admin = admin;
      this.name = name;

      users = new Dictionary<string, RoomUser>();
      messages = new Dictionary<long, Message>();
      files = new List<FileDescription>();

      if (admin != null)
        users.Add(admin, new RoomUser(admin));
    }

    /// <summary>
    /// Создает комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    /// <param name="initialUsers">Начальный список пользователей комнаты.</param>
    public Room(string admin, string name, IEnumerable<User> initialUsers)
      : this(admin, name)
    {
      foreach(var user in initialUsers)
      {
        if (string.Equals(admin, user.Nick))
          continue;

        users.Add(user.Nick, new RoomUser(user.Nick));
      }
    }

    /// <summary>
    /// Название комнаты.
    /// </summary>
    public string Name
    {
      get { return name; }
    }

    /// <summary>
    /// Администратор комнаты.
    /// </summary>
    public string Admin
    {
      get { return admin; }
      set { admin = value; }
    }

    /// <summary>
    /// Список пользователей комнаты, включая администратора.
    /// </summary>
    public ICollection<string> Users
    {
      get { return users.Keys; }
    }

    /// <summary>
    /// Количество пользователей.
    /// </summary>
    public int Count
    {
      get { return users.Count; }
    }

    /// <summary>
    /// Файлы раздающиеся в комнате.
    /// </summary>
    public List<FileDescription> Files
    {
      get { return files; }
    }

    /// <summary>
    /// Добавляет пользователя в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public virtual void AddUser(string nick)
    {
      users.Add(nick, new RoomUser(nick));
    }

    /// <summary>
    /// Удаляет пользователя из комнаты.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public virtual void RemoveUser(string nick)
    {
      users.Remove(nick);
    }

    /// <summary>
    /// Добавляет сообщение в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя написавшего сообщение.</param>
    /// <param name="text">Текст сообщения</param>
    /// <returns>Добавленное сообщение.</returns>
    public Message AddMessage(string nick, string text)
    {
      var message = AddMessage(nick, lastMessageId, text);
      if (message.Id == lastMessageId)
        lastMessageId++;

      return message;
    }

    /// <summary>
    /// Добавляет сообщение в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя написавшего сообщение.</param>
    /// <param name="messageId">Идентификатор сообщения.</param>
    /// <param name="text">Текст сообщения</param>
    /// <returns>Добавленное сообщение.</returns>
    public Message AddMessage(string nick, long messageId, string text)
    {
      var user = GetUser(nick);
      if (user == null)
        throw new ArgumentException("nick");

      var message = new Message(nick, messageId, text);
      var lastMessage = GetMessage(lastMessageId - 1);

      if (lastMessage != null && lastMessage.TryConcat(message))
        return lastMessage;

      user.AddMessageId(message.Id);
      messages[message.Id] = message;
      return message;
    }

    /// <summary>
    /// Возвращает сообщение. Если оно есть.
    /// </summary>
    /// <param name="messageId">Идентификатор сообщения.</param>
    /// <returns>Сообщение.</returns>
    public Message GetMessage(long messageId)
    {
      Message message;
      messages.TryGetValue(messageId, out message);
      return message;
    }

    /// <summary>
    /// Есть ли пользователь в комнате.
    /// </summary>
    /// <param name="nick">Ник пользователя</param>
    public bool ContainsUser(string nick)
    {
      return users.ContainsKey(nick);
    }

    /// <summary>
    /// Возвращает пользователя комнаты.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    /// <returns>Пользователь команты.</returns>
    public RoomUser GetUser(string nick)
    {
      RoomUser user;
      users.TryGetValue(nick, out user);
      return user;
    }

    /// <summary>
    /// Сравнивает этот объект с объектом указанным в параметре метода.
    /// </summary>
    /// <param name="obj">Объект с которомы осуществляется сравнение.</param>
    /// <returns>Истина если объекты равны.</returns>
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;

      if (ReferenceEquals(obj, this))
        return true;

      var room = obj as Room;
      if (ReferenceEquals(room, null))
        return false;

      return Equals(room);
    }

    public override int GetHashCode()
    {
      return name.GetHashCode();
    }

    /// <summary>
    /// Сравнивает этот объект с объектом указанным в параметре метода.
    /// </summary>
    /// <param name="room">Объект с которомы осуществляется сравнение.</param>
    /// <returns>Истина если объекты равны.</returns>
    public bool Equals(Room room)
    {
      return string.Equals(name, room.name);
    }
  }
}
