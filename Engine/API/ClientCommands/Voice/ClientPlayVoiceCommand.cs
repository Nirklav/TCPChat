using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Security;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientPlayVoiceCommand :
    ICommand<ClientCommandArgs>
  {
    public static ushort CommandId = (ushort)ClientCommand.PlayVoice;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);
      ClientModel.Player.Enqueue(args.PeerConnectionId, receivedContent.Number, receivedContent.Pack);
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
