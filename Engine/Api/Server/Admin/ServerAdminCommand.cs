using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using Engine.Model.Server.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Admin
{
  [SecurityCritical]
  public class ServerAdminCommand :
    ServerCommand<ServerAdminCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.Admin;

    private const char TextCommandStart = '/';
    private static readonly Dictionary<string, Action> TextCommands = new Dictionary<string, Action>
    {
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

      Action command;
      if (!TextCommands.TryGetValue(content.TextCommand, out command))
      {
        ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.TextCommandNotFound));
        return;
      }

      command();
    }

    private static void ClearMainRoom()
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
