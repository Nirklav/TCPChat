using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientOutPrivateMessageCommand :
      BaseCommand,
      IClientCommand
  {
    public void Run(ClientCommandArgs args)
    {
      MessageContent receivedContent = GetContentFromMessage<MessageContent>(args.Message);

      if (receivedContent.Key == null)
        throw new ArgumentNullException("key");

      if (receivedContent.Message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(receivedContent.Sender))
        throw new ArgumentException("sender");

      byte[] decryptedSymmetricKey = ClientModel.Client.KeyCryptor.Decrypt(receivedContent.Key, false);

      using (MemoryStream messageStream = new MemoryStream(),
             encryptedMessageStream = new MemoryStream(receivedContent.Message))
      {
        AesCryptoServiceProvider provider = new AesCryptoServiceProvider 
        { 
          Padding = PaddingMode.Zeros, 
          Mode = CipherMode.CBC 
        };

        using(Crypter messageCrypter = new Crypter(provider))
          messageCrypter.DecryptStream(encryptedMessageStream, messageStream, decryptedSymmetricKey);

        ReceiveMessageEventArgs receiveMessageArgs = new ReceiveMessageEventArgs
        {
          Type = MessageType.Private,
          Message = Encoding.Unicode.GetString(messageStream.ToArray()),
          Sender = receivedContent.Sender,
        };

        ClientModel.OnReceiveMessage(this, receiveMessageArgs);
      }
    }

    [Serializable]
    public class MessageContent
    {
      byte[] key;
      byte[] message;
      string sender;

      public byte[] Key { get { return key; } set { key = value; } }
      public byte[] Message { get { return message; } set { message = value; } }
      public string Sender { get { return sender; } set { sender = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.OutPrivateMessage;
  }
}
