using System;
using System.Linq;
using System.Collections.Generic;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий комнату.
  /// </summary>
  [Serializable]
  public class Room : IEquatable<Room>
  {
    /// <summary>
    /// Идентификатор сообщений которые невозможно редактировать.
    /// </summary>
    public const long SpecificMessageId = -1;

    protected readonly string name;
    protected readonly List<string> users;
    protected readonly Dictionary<long, Message> messages;
    protected readonly List<FileDescription> files;

    protected string admin;
    protected long lastMessageId;

    [NonSerialized]
    private bool enabled; // Only client

    /// <summary>
    /// Создает комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    public Room(string admin, string name)
    {
      this.admin = admin;
      this.name = name;

      users = new List<string>();
      messages = new Dictionary<long, Message>();
      files = new List<FileDescription>();
      enabled = true;

      if (admin != null)
        users.Add(admin);
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
      users.AddRange(initialUsers.Select(u => u.Nick).Where(n => n != admin));
    }

    /// <summary>
    /// Тип комнаты.
    /// </summary>
    public virtual RoomType Type
    {
      get { return RoomType.Chat; }
    }

    /// <summary>
    /// Включенность комнаты.
    /// </summary>
    public bool Enabled
    {
      get { return enabled; }
      set { enabled = value; }
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
      get { return users; }
    }

    /// <summary>
    /// Количество пользователей.
    /// </summary>
    public ICollection<Message> Messages
    {
      get { return messages.Values; }
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
      users.Add(nick);
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
      var message = new Message(nick, messageId, text);
      var lastMessage = GetMessage(lastMessageId - 1);

      if (lastMessage != null && lastMessage.TryConcat(message))
        return lastMessage;

      messages[message.Id] = message;
      return message;
    }

    public void AddMessage(Message message)
    {
      messages[message.Id] = message;
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
      return users.Contains(nick);
    }

    /// <summary>
    /// Проверяет принадлежит ли сообщение пользователю.
    /// </summary>
    /// <param name="nick">Ник.</param>
    /// <param name="messageId">Идентификатор сообщения.</param>
    /// <returns>Принадлежит ли сообщение пользователю</returns>
    public bool IsMessageBelongToUser(string nick, long messageId)
    {
      var message = GetMessage(messageId);
      if (message == null)
        return false;

      return string.Equals(nick, message.Owner);
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
      if (ReferenceEquals(room, null))
        return false;

      if (ReferenceEquals(room, this))
        return true;

      return string.Equals(name, room.name);
    }
  }
}
