using Engine.Helpers;
using Engine.Model.Client;
using Engine.Model.Entities;
using System;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientFilePostedCommand :
      ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.File == null)
        throw new ArgumentNullException("file");

      if (string.IsNullOrEmpty(receivedContent.RoomName))
        throw new ArgumentException("roomName");

      ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs
      {
        Type = MessageType.File,
        Message = receivedContent.File.Name,
        Sender = receivedContent.File.Owner.Nick,
        RoomName = receivedContent.RoomName,
        State = receivedContent.File,
      };

      ClientModel.OnReceiveMessage(this, receiveMessageArgs);
    }

    [Serializable]
    public class MessageContent
    {
      FileDescription file;
      string roomName;

      public FileDescription File { get { return file; } set { file = value; } }
      public string RoomName { get { return roomName; } set { roomName = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.FilePosted;
  }
}
