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
    private string name;
    private string admin;
    private List<string> users;
    private List<FileDescription> files;

    /// <summary>
    /// Создает комнату.
    /// </summary>
    /// <param name="adminNick">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    public Room(string admin, string name)
    {
      this.admin = admin;
      this.name = name;

      users = new List<string>();
      files = new List<FileDescription>();

      if (admin != null)
        users.Add(admin);
    }

    /// <summary>
    /// Создает комнату.
    /// </summary>
    /// <param name="adminNick">Ник администратора комнаты.</param>
    /// <param name="name">Название комнаты.</param>
    /// <param name="users">Начальный список пользователей комнаты. Уже существуюшие пользователе повторно добавлены не будут.</param>
    public Room(string admin, string name, IEnumerable<User> initialUsers)
      : this(admin, name)
    {
      users.AddRange(initialUsers.Where(u => !string.Equals(admin, u.Nick)).Select(u => u.Nick));
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
    public List<string> Users
    {
      get { return users; }
    }

    /// <summary>
    /// Файлы раздающиеся в комнате.
    /// </summary>
    public List<FileDescription> Files
    {
      get { return files; }
    }

    /// <summary>
    /// Сравнивает этот объект с объектом указанным в параметре метода.
    /// </summary>
    /// <param name="obj">Объект с которомы осуществляется сравнение.</param>
    /// <returns>Истина если объекты равны.</returns>
    public override bool Equals(object obj)
    {
      if (!(obj is Room))
        return false;

      return Equals((Room)obj);
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
