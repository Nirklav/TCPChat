using Engine.API.ClientCommands;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network.Connections;
using System;
using System.Linq;
using System.Security;

namespace Engine.API.ServerCommands
{
  [SecurityCritical]
  class ServerRegisterCommand :
    ServerCommand<ServerRegisterCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.Register;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, ServerCommandArgs args)
    {
      if (content.User == null)
        throw new ArgumentNullException("User");

      if (content.User.Nick == null)
        throw new ArgumentNullException("User.Nick");

      if (content.User.Nick.Contains(Connection.TempConnectionPrefix))
      {
        SendFail(args.ConnectionId, MessageId.NotRegisteredBadName);
        return;
      }
      
      using (var server = ServerModel.Get())
      {
        var room = server.Rooms[ServerModel.MainRoomName];    
        var userExist = room.Users.Any(nick => string.Equals(content.User.Nick, nick));

        if (userExist)
        {
          SendFail(args.ConnectionId, MessageId.NotRegisteredNameAlreadyExist);
          return;
        }
        else
        {
          ServerModel.Logger.WriteInfo("User login: {0}", content.User.Nick);

          server.Users.Add(content.User.Nick, content.User);
          room.AddUser(content.User.Nick);

          var regResponseContent = new ClientRegistrationResponseCommand.MessageContent { Registered = true };

          ServerModel.Server.RegisterConnection(args.ConnectionId, content.User.Nick);
          ServerModel.Server.SendMessage(content.User.Nick, ClientRegistrationResponseCommand.CommandId, regResponseContent);

          var sendingContent = new ClientRoomRefreshedCommand.MessageContent
          {
            Room = room,
            Users = room.Users.Select(nick => server.Users[nick]).ToList()
          };

          foreach (var connectionId in room.Users)
            ServerModel.Server.SendMessage(connectionId, ClientRoomRefreshedCommand.CommandId, sendingContent);

          ServerModel.Notifier.Registered(new ServerRegistrationEventArgs { Nick = content.User.Nick });
        }
      }
    }

    private void SendFail(string connectionId, MessageId message)
    {
      var regResponseContent = new ClientRegistrationResponseCommand.MessageContent { Registered = false, Message = message };
      ServerModel.Server.SendMessage(connectionId, ClientRegistrationResponseCommand.CommandId, regResponseContent, true);
      ServerModel.Api.RemoveUser(connectionId);
    }

    [Serializable]
    public class MessageContent
    {
      private User user;

      public User User
      {
        get { return user; }
        set { user = value; }
      }
    }
  }
}
