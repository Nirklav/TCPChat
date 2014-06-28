using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.API.StandardAPI.ClientCommands.Voice
{
  class ClientPlayVoiceCommand : 
    BaseCommand, 
    IClientAPICommand
  {
    public void Run(ClientCommandArgs args)
    {
      if (args.PeerConnectionId == null)
        return;

      MessageContent receivedContent = GetContentFormMessage<MessageContent>(args.Message);
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
