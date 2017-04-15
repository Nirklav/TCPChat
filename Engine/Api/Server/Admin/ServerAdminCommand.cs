using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Admin
{
  [SecurityCritical]
  public class ServerAdminCommand :
    ServerCommand<ServerAdminCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.Admin;

    private delegate void AdminCommand(MessageContent content, CommandArgs args);

    private const char TextCommandStart = '/';
    private static readonly Dictionary<string, AdminCommand> TextCommands = new Dictionary<string, AdminCommand>
    {
      { "/list", List },
      { "/clearMainRoom", ClearMainRoom }
    };

    private readonly string _password;

    public ServerAdminCommand(string password)
    {
      _password = password;
    }

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (!string.Equals(content.Password, _password, StringComparison.Ordinal))
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.InvalidPassword));
        return;
      }

      AdminCommand command;
      if (!TextCommands.TryGetValue(content.TextCommand, out command))
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandNotFound));
        return;
      }

      command(content, args);
    }

    private static void ClearMainRoom(MessageContent content, CommandArgs args)
    {
      using (var server = ServerModel.Get())
      {
        var room = server.Chat.GetRoom(ServerChat.MainRoomName);
        var messageIds = room.Messages.Select(m => m.Id).ToArray();

        room.RemoveMessages(messageIds);

        foreach (var nick in room.Users)
          ServerModel.Api.Perform(new ServerRemoveMessagesAction(nick, room.Name, messageIds));
      }
    }

    private static void List(MessageContent content, CommandArgs args)
    {
      var commandsBuilder = new StringBuilder();
      commandsBuilder.AppendLine();
      foreach (var command in TextCommands.Keys)
        commandsBuilder.AppendLine(command);

      ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandsList, commandsBuilder.ToString()));
    }

    [Serializable]
    [BinType("ServerAdmin")]
    public class MessageContent
    {
      [BinField("t")]
      public string TextCommand;

      [BinField("p")]
      public string Password;
    }

    public static bool IsTextCommand(string command)
    {
      return command[0] == TextCommandStart && TextCommands.ContainsKey(command);
    }
  }
}
