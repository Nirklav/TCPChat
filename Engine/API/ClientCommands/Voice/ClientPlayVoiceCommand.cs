using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;

namespace Engine.API.ClientCommands
{
  class ClientPlayVoiceCommand :
    ICommand<ClientCommandArgs>
  {
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
      SoundPack pack;
      long number;

      public SoundPack Pack { get { return pack; } set { pack = value; } }
      public long Number { get { return number; } set { number = value; } }
    }

    public static ushort Id = (ushort)ClientCommand.PlayVoice;
  }
}
