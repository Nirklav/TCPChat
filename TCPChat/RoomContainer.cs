using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using TCPChat.Engine;

namespace TCPChat
{
    public class RoomContainer : INotifyPropertyChanged
    {
        private bool updated;
        private const int overflow = 250;

        public string RoomName
        {
            get { return Info.Name; }
        }

        public RoomDescription Info { get; private set; }
        public ObservableCollection<MessageContainer> MessagesCollection { get; private set; }
        public ObservableCollection<UserContainer> UsersCollection { get; private set; }
        public bool Updated
        {
            get { return updated; }
            set
            {
                updated = value;
                OnPropertyChanged("Updated");
            }
        }

        public RoomContainer(RoomDescription room, NotifyCollectionChangedEventHandler handler)
        {
            MessagesCollection = new ObservableCollection<MessageContainer>();
            MessagesCollection.CollectionChanged += handler;
            UsersCollection = new ObservableCollection<UserContainer>();
            Info = room;
            updated = false;
        }

        public RoomContainer(RoomDescription room, string clientNick, NotifyCollectionChangedEventHandler handler)
            : this(room, handler)
        {
            Refresh(room, clientNick);
        }

        public void Refresh(RoomDescription room, string clientNick)
        {
            Info = room;
            UsersCollection.Clear();

            foreach (UserDescription user in room.Users)
            {
                if (string.Equals(user.Nick, clientNick))
                    UsersCollection.Add(new UserContainer(user, true));
                else
                    UsersCollection.Add(new UserContainer(user, false));
            }
        }

        public void AddFileMessage(UserContainer sender, string fileName, FileDescription file)
        {
            MessagesCollection.Add(new MessageContainer(sender, fileName, file));

            VerifyOverflowInMessages();
        }

        public void AddSystemMessage(string Message)
        {
            MessagesCollection.Add(new MessageContainer(Message));

            VerifyOverflowInMessages();
        }

        public void AddMessage(UserContainer sender, UserContainer receiver, string Message, bool IsPrivate)
        {
            if (sender == null || receiver == null)
                return;

            MessagesCollection.Add(new MessageContainer(sender, receiver, Message, IsPrivate));

            VerifyOverflowInMessages();
        }

        private void VerifyOverflowInMessages()
        {
            if (MessagesCollection.Count < overflow)
                return;

            IEnumerable<MessageContainer> savedMessages = MessagesCollection.Skip(overflow / 2).ToArray();
            MessagesCollection.Clear();

            foreach (MessageContainer current in savedMessages)
                MessagesCollection.Add(current);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RoomContainer))
                return false;

            return Equals((RoomContainer)obj);
        }

        public bool Equals(RoomContainer user)
        {
            return user.RoomName.Equals(RoomName);
        }

        public override int GetHashCode()
        {
            return RoomName.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler temp = Interlocked.CompareExchange<PropertyChangedEventHandler>(ref PropertyChanged, null, null);

            if (temp != null) 
                temp(this, new PropertyChangedEventArgs(name));
        }
    }
}
