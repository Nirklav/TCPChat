using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChat.Engine
{
    /// <summary>
    /// Класс описывающий комнату.
    /// </summary>
    [Serializable]
    public class RoomDescription
    {
        private string name;
        private UserDescription admin;
        private List<UserDescription> users;
        private List<FileDescription> files;

        /// <summary>
        /// Создает комнату.
        /// </summary>
        /// <param name="adminNick">Ник администратора комнаты.</param>
        /// <param name="name">Название комнаты.</param>
        public RoomDescription(UserDescription admin, string name)
        {
            this.admin = admin;
            this.name = name;

            users = new List<UserDescription>();
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
        public RoomDescription(UserDescription admin, string name, IEnumerable<UserDescription> users)
            : this(admin, name)
        {
            this.users.AddRange(users.Where((user) => !string.Equals(admin.Nick, user.Nick)));
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
        public UserDescription Admin
        {
            get { return admin; }
            set { admin = value; }
        }

        /// <summary>
        /// Список пользователей комнаты, включая администратора.
        /// </summary>
        public List<UserDescription> Users
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
            if (!(obj is RoomDescription))
                return false;

            return Equals((RoomDescription)obj);
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
        public bool Equals(RoomDescription room)
        {
            return string.Equals(name, room.name);
        }
    }
}
