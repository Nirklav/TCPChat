using System;
using System.Security;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Client.Voice
{
  [SecurityCritical]
  class ClientPlayVoiceCommand :
    ClientCommand<ClientPlayVoiceCommand.MessageContent>
  {
    public static long CommandId = (long)ClientCommandId.PlayVoice;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    protected override bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return true; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      using (var client = ClientModel.Get())
      {
        var user = client.Chat.GetUser(args.ConnectionId);
        if (user.IsVoiceActive())
          ClientModel.Player.Enqueue(args.ConnectionId, content.Number, content.Pack);
      }
    }

    [Serializable]
    [BinType("ClientPlayVoice")]
    public class MessageContent
    {
      [BinField("p")]
      public SoundPack Pack;

      [BinField("n")]
      public long Number;
    }
  }
}
