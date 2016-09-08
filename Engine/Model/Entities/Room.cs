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

    protected readonly string _name;
    protected readonly List<string> _users;
    protected readonly Dictionary<long, Message> _messages;
    protected readonly List<FileDescription> _files;

    protected string _admin;
    protected long _lastMessageId;

    [NonSerialized]
    private bool _enabled; // Only client

    /// <summary>
    /// Создает комнату.
    /// </summary>
    /// <param name="admin">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    public Room(string admin, string name)
    {
      _admin = admin;
      _name = name;
      _users = new List<string>();
      _messages = new Dictionary<long, Message>();
      _files = new List<FileDescription>();
      _enabled = true;

      if (admin != null)
        _users.Add(admin);
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
      _users.AddRange(initialUsers.Select(u => u.Nick).Where(n => n != admin));
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
      get { return _enabled; }
      set { _enabled = value; }
    }

    /// <summary>
    /// Название комнаты.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Администратор комнаты.
    /// </summary>
    public string Admin
    {
      get { return _admin; }
      set { _admin = value; }
    }

    /// <summary>
    /// Список пользователей комнаты, включая администратора.
    /// </summary>
    public ICollection<string> Users
    {
      get { return _users; }
    }

    /// <summary>
    /// Количество пользователей.
    /// </summary>
    public ICollection<Message> Messages
    {
      get { return _messages.Values; }
    }

    /// <summary>
    /// Файлы раздающиеся в комнате.
    /// </summary>
    public List<FileDescription> Files
    {
      get { return _files; }
    }

    /// <summary>
    /// Добавляет пользователя в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public virtual void AddUser(string nick)
    {
      _users.Add(nick);
    }

    /// <summary>
    /// Удаляет пользователя и его файлы из комнаты.
    /// </summary>
    /// <param name="nick">Ник пользователя.</param>
    public virtual void RemoveUser(string nick)
    {
      _users.Remove(nick);
      _files.RemoveAll(f => f.Id.Owner == nick);
    }

    /// <summary>
    /// Добавляет сообщение в комнату.
    /// </summary>
    /// <param name="nick">Ник пользователя написавшего сообщение.</param>
    /// <param name="text">Текст сообщения</param>
    /// <returns>Добавленное сообщение.</returns>
    public Message AddMessage(string nick, string text)
    {
      var message = AddMessage(nick, _lastMessageId, text);
      if (message.Id == _lastMessageId)
        _lastMessageId++;

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
      var lastMessage = GetMessage(_lastMessageId - 1);

      if (lastMessage != null && lastMessage.TryConcat(message))
        return lastMessage;

      _messages[message.Id] = message;
      return message;
    }

    public void AddMessage(Message message)
    {
      _messages[message.Id] = message;
    }

    /// <summary>
    /// Возвращает сообщение. Если оно есть.
    /// </summary>
    /// <param name="messageId">Идентификатор сообщения.</param>
    /// <returns>Сообщение.</returns>
    public Message GetMessage(long messageId)
    {
      Message message;
      _messages.TryGetValue(messageId, out message);
      return message;
    }

    /// <summary>
    /// Есть ли пользователь в комнате.
    /// </summary>
    /// <param name="nick">Ник пользователя</param>
    public bool ContainsUser(string nick)
    {
      return _users.Contains(nick);
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
      return _name.GetHashCode();
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

      return string.Equals(_name, room._name);
    }
  }
}
