using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TCPChat.Engine
{
    /// <summary>
    /// Описание пользователя.
    /// </summary>
    [Serializable]
    public class UserDescription
    {
        private string nick;
        private Color nickColor;

        /// <summary>
        /// Создает описание пользователя.
        /// </summary>
        /// <param name="Nick">Ник пользователя.</param>
        public UserDescription(string Nick)
        {
            nick = Nick;
        }

        /// <summary>
        /// Возвращает ник пользователя.
        /// </summary>
        public string Nick
        {
            get { return nick; }
            set { nick = value; }
        }

        /// <summary>
        /// Цвет ника пользователя.
        /// </summary>
        public Color NickColor
        {
            get { return nickColor; }
            set { nickColor = value; }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UserDescription))
                return false;

            return Equals((UserDescription)obj);
        }

        public override int GetHashCode()
        {
            return nick.GetHashCode();
        }

        public bool Equals(UserDescription user)
        {
            return string.Equals(Nick, user.Nick);
        }
    }
}
