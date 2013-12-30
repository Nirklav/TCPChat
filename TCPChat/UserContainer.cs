using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using WPFColor = System.Windows.Media.Color;
using TCPChat.Engine;

namespace TCPChat
{
    public class UserContainer
    {
        public UserContainer(UserDescription userInfo, bool Me)
        {
            Info = userInfo;
            ItsMe = Me;
        }

        public UserDescription Info { get; private set; }

        public bool ItsMe { get; set; }

        public string Nick
        {
            get { return Info.Nick; }
            set { }
        }

        public WPFColor NickColor
        {
            get { return WPFColor.FromArgb(Info.NickColor.A, Info.NickColor.R, Info.NickColor.G, Info.NickColor.B); }
            set { Info.NickColor = Color.FromArgb(value.A, value.R, value.G, value.B); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is UserContainer))
                return false;

            return Equals((UserContainer)obj);
        }

        public bool Equals(UserContainer user)
        {
            if (user == null)
                return false;

            return user.Info.Equals(Info);
        }

        public override int GetHashCode()
        {
            return Info.GetHashCode();
        }
    }
}
