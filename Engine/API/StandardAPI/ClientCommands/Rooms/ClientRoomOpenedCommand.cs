﻿using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientRoomOpenedCommand :
      BaseCommand,
      IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.Room == null)
        throw new ArgumentNullException("room");

      ClientModel.OnRoomOpened(this, new RoomEventArgs { Room = receivedContent.Room, Users = receivedContent.Users });
    }

    [Serializable]
    public class MessageContent
    {
      Room room;
      List<User> users;

      public Room Room { get { return room; } set { room = value; } }
      public List<User> Users { get { return users; } set { users = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.RoomOpened;
  }
}