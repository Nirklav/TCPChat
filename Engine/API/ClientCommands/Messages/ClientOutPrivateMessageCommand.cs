using Engine.Helpers;
using Engine.Model.Client;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Engine.API.ClientCommands
{
  [SecurityCritical]
  class ClientOutPrivateMessageCommand :
    ICommand<ClientCommandArgs>
  {
    public const ushort CommandId = (ushort)ClientCommand.OutPrivateMessage;

    public ushort Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    public void Run(ClientCommandArgs args)
    {
      var receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (receivedContent.Key == null)
        throw new ArgumentNullException("key");

      if (receivedContent.Message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(receivedContent.Sender))
        throw new ArgumentException("sender");

      var decryptedSymmetricKey = ClientModel.Client.KeyCryptor.Decrypt(receivedContent.Key, true);

      using (MemoryStream messageStream = new MemoryStream(),
             encryptedMessageStream = new MemoryStream(receivedContent.Message))
      {
        var provider = new AesCryptoServiceProvider 
        { 
          Padding = PaddingMode.Zeros, 
          Mode = CipherMode.CBC 
        };

        using(Crypter messageCrypter = new Crypter(provider))
          messageCrypter.DecryptStream(encryptedMessageStream, messageStream, decryptedSymmetricKey);

        var receiveMessageArgs = new ReceiveMessageEventArgs
        {
          Type = MessageType.Private,
          Message = Encoding.Unicode.GetString(messageStream.ToArray()),
          Sender = receivedContent.Sender,
        };

        ClientModel.Notifier.ReceiveMessage(receiveMessageArgs);
      }
    }

    [Serializable]
    public class MessageContent
    {
      private byte[] key;
      private byte[] message;
      private string sender;

      public byte[] Key
      {
        get { return key; }
        set { key = value; }
      }

      public byte[] Message
      {
        get { return message; }
        set { message = value; }
      }

      public string Sender
      {
        get { return sender; }
        set { sender = value; }
      }
    }
  }
}
