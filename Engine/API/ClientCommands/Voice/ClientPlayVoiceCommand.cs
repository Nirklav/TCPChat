using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
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
    protected override void OnRun(MessageContent content, ClientCommandArgs args)
    {
      if (ClientModel.Api.IsActiveInterlocutor(args.PeerConnectionId))
        ClientModel.Player.Enqueue(args.PeerConnectionId, content.Number, content.Pack);
    }

    [Serializable]
    public class MessageContent
    {
      private SoundPack pack;
      private long number;

      public SoundPack Pack
      {
        get { return pack; }
        set { pack = value; }
      }

      public long Number
      {
        get { return number; }
        set { number = value; }
      }
    }
  }
}
