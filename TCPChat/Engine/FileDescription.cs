using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChat.Engine
{
    /// <summary>
    /// Класс описывающий файл раздаваемый пользователем.
    /// </summary>
    [Serializable]
    public class FileDescription
    {
        private UserDescription owner;
        private int id;
        private string name;
        private long size;

        /// <summary>
        /// Создает новый экземпляр класса.
        /// </summary>
        /// <param name="fileOwner">Пользователь раздающий файл.</param>
        /// <param name="fileName">Короткое имя файла.</param>
        /// <param name="fileID">Индетификатор файла. В пределах пользователя должен быть уникален.</param>
        public FileDescription(UserDescription fileOwner, long fileSize, string fileName, int fileID)
        {
            owner = fileOwner;
            id = fileID;
            name = fileName;
            size = fileSize;
        }

        /// <summary>
        /// Пользователь раздающий файл.
        /// </summary>
        public UserDescription Owner
        {
            get { return owner; }
        }

        /// <summary>
        /// Имя файла.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Размер файла.
        /// </summary>
        public long Size
        {
            get { return size; }
        }

        /// <summary>
        /// Идентификатор файла.
        /// </summary>
        public int ID
        {
            get { return id; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            FileDescription file = obj as FileDescription;

            if (file == null)
                return false;

            return Equals(file);
        }

        public bool Equals(FileDescription file)
        {
            if (file == null)
                return false;

            return owner.Equals(file.owner) && id.Equals(file.id);
        }

        public override int GetHashCode()
        {
            return owner.GetHashCode() ^ id.GetHashCode();
        }
    }
}
