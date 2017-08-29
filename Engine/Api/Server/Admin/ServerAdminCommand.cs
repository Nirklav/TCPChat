using Engine.Api.Server.Messages;
using Engine.Api.Server.Registrations;
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
    private struct AdminCommandArgs
    {
      private static readonly string[] Empty = new string[0];

      public readonly string Command;
      public readonly string[] Parameters;

      public AdminCommandArgs(string textCommand)
      {
        var trimed = textCommand.Trim();
        Command = GetCommand(trimed);
        Parameters = GetParameters(trimed);
      }

      private static string GetCommand(string textCommand)
      {
        var firstSpaceIndex = textCommand.IndexOf(' ');
        if (firstSpaceIndex < 0)
          return textCommand;
        return textCommand.Substring(0, firstSpaceIndex);
      }

      private static string[] GetParameters(string textCommand)
      {
        var firstSpaceIndex = textCommand.IndexOf(' ');
        if (firstSpaceIndex < 0)
          return Empty;

        var escaped = false;
        var builder = new StringBuilder();
        var result = new List<string>();

        for (int i = firstSpaceIndex + 1; i < textCommand.Length;  i++)
        {
          var ch = textCommand[i];
          switch (ch)
          {
            case '"':
              escaped = !escaped;
              break;
            case ' ':
              if (escaped)
                builder.Append(ch);
              else
              {
                result.Add(builder.ToString());
                builder.Clear();
              }
              break;
            default:
              builder.Append(ch);
              break;
          }
        }

        if (builder.Length > 0)
          result.Add(builder.ToString());

        return result.ToArray();
      }
    }

    private struct AdminCommand
    {
      public delegate void Command(AdminCommandArgs adminArgs, CommandArgs args);

      private readonly Command _command;
      private readonly string _description;
      
      public AdminCommand(Command command, string description)
      {
        _command = command;
        _description = description;
      }

      [SecuritySafeCritical]
      public void Run(AdminCommandArgs adminArgs, CommandArgs args)
      {
        _command(adminArgs, args);
      }

      [SecuritySafeCritical]
      public override string ToString()
      {
        return _description;
      }
    }

    public const long CommandId = (long)ServerCommandId.Admin;

    private const char TextCommandStart = '/';
    private static readonly Dictionary<string, AdminCommand> TextCommands = new Dictionary<string, AdminCommand>(StringComparer.OrdinalIgnoreCase)
    {
      { "/list", new AdminCommand(Help, "/list - shows list of commands") },
      { "/help", new AdminCommand(Help, "/help - shows list of commands") },
      { "/clearMainRoom", new AdminCommand(ClearMainRoom, "/clearMainRoom - clears main room from messages") },
      { "/clearRoom", new AdminCommand(ClearRoom, "/clearRoom {roomName} - clears room that received in params from messages") },
      { "/kick",  new AdminCommand(Kick, "/kick {nick} - kicks user from chat") },
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

      var adminArgs = new AdminCommandArgs(content.TextCommand);

      AdminCommand command;
      if (!TextCommands.TryGetValue(adminArgs.Command, out command))
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandNotFound));
        return;
      }

      command.Run(adminArgs, args);
    }

    [SecuritySafeCritical]
    private static void ClearMainRoom(AdminCommandArgs adminArgs, CommandArgs args)
    {
      ClearRoom(ServerChat.MainRoomName, args);
    }

    [SecuritySafeCritical]
    private static void ClearRoom(AdminCommandArgs adminArgs, CommandArgs args)
    {
      if (adminArgs.Parameters.Length == 1)
        ClearRoom(adminArgs.Parameters[0], args);
      else
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandInvalidParams));
    }

    [SecuritySafeCritical]
    private static void ClearRoom(string name, CommandArgs args)
    {
      using (var server = ServerModel.Get())
      {
        var room = server.Chat.TryGetRoom(name);
        if (room == null)
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandInvalidParams));
        else
        {
          var messageIds = room.Messages.Select(m => m.Id).ToArray();

          room.RemoveMessages(messageIds);

          foreach (var nick in room.Users)
            ServerModel.Api.Perform(new ServerRemoveMessagesAction(nick, room.Name, messageIds));
        }
      }
    }

    [SecuritySafeCritical]
    private static void Help(AdminCommandArgs adminArgs, CommandArgs args)
    {
      var commandsBuilder = new StringBuilder();
      commandsBuilder.AppendLine();
      foreach (var command in TextCommands.Values)
        commandsBuilder.AppendLine(command.ToString());

      ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandsList, commandsBuilder.ToString()));
    }

    [SecuritySafeCritical]
    private static void Kick(AdminCommandArgs adminArgs, CommandArgs args)
    {
      if (adminArgs.Parameters.Length != 1)
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandInvalidParams));
        return;
      }

      using (var server = ServerModel.Get())
      {
        var nick = adminArgs.Parameters[0];
        var user = server.Chat.TryGetUser(nick);
        if (user != null)
          ServerModel.Api.Perform(new ServerRemoveUserAction(nick));  
        else
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandInvalidParams));
      }
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
      return command[0] == TextCommandStart && TextCommands.ContainsKey(new AdminCommandArgs(command).Command);
    }
  }
}
