using Engine.API.StandardAPI.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Engine.API.StandardAPI.ServerCommands
{
  class ServerRegisterCommand :
      BaseServerCommand,
      IServerAPICommand
  {
    public void Run(ServerCommandArgs args)
    {
      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);

      if (receivedContent.User == null)
        throw new ArgumentNullException("User");

      using (var server = ServerModel.Get())
      {
        Room room = server.Rooms[ServerModel.MainRoomName];

        bool userExist = false;
        foreach (var nick in room.Users)
          if (string.Equals(receivedContent.User.Nick, nick))
          {
            userExist = true;
            break;
          }

        if (!userExist)
        {
          ServerModel.Server.RegisterConnection(args.ConnectionId, receivedContent.User.Nick, receivedContent.OpenKey);

          server.Users.Add(receivedContent.User.Nick, receivedContent.User);
          room.Add(receivedContent.User.Nick);

          var regResponseContent = new ClientRegistrationResponseCommand.MessageContent { Registered = !userExist };
          ServerModel.Server.SendMessage(args.ConnectionId, ClientRegistrationResponseCommand.Id, regResponseContent);

          foreach (var connectionId in ServerModel.Server.GetConnetionsIds())
          {
            var sendingContent = new ClientRoomRefreshedCommand.MessageContent
            {
              Room = room,
              Users = room.Users.Select(nick => server.Users[nick]).ToList()
            };
            ServerModel.Server.SendMessage(connectionId, ClientRoomRefreshedCommand.Id, sendingContent);
          }
        }
        else
        {
          var regResponseContent = new ClientRegistrationResponseCommand.MessageContent { Registered = !userExist };
          ServerModel.Server.SendMessage(args.ConnectionId, ClientRegistrationResponseCommand.Id, regResponseContent);
          ServerModel.API.CloseConnection(args.ConnectionId);
        }
      }
    }

    [Serializable]
    public class MessageContent
    {
      RSAParameters openKey;
      User user;

      public RSAParameters OpenKey { get { return openKey; } set { openKey = value; } }
      public User User { get { return user; } set { user = value; } }
    }

    public const ushort Id = (ushort)ServerCommand.Register;
  }
}
