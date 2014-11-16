using Engine.API.StandardAPI.ServerCommands;
using Engine.Containers;
using Engine.Helpers;
using Engine.Model.Client;
using Engine.Network;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Engine.API.StandardAPI.ClientCommands
{
  class ClientReceiveUserOpenKeyCommand :
      ICommand<ClientCommandArgs>
  {
    public void Run(ClientCommandArgs args)
    {
      StandardClientAPI API = (StandardClientAPI)ClientModel.API;
      MessageContent receivedContent = Serializer.Deserialize<MessageContent>(args.Message);

      if (string.IsNullOrEmpty(receivedContent.Nick))
        throw new ArgumentException("Nick");

      WaitingPrivateMessage waitingMessage;
      lock (API.WaitingPrivateMessages)
      {
        waitingMessage = API.WaitingPrivateMessages.Find(m => m.Receiver.Equals(receivedContent.Nick));
        if (waitingMessage == null)
          return;

        API.WaitingPrivateMessages.Remove(waitingMessage);
      }

      var sendingContent = new ServerSendPrivateMessageCommand.MessageContent { Receiver = receivedContent.Nick };
      AesCryptoServiceProvider provider = new AesCryptoServiceProvider
      {
        Padding = PaddingMode.Zeros,
        Mode = CipherMode.CBC
      };

      using (Crypter messageCrypter = new Crypter(provider))
      {
        byte[] symmetricKey = messageCrypter.GenerateKey();

        RSACryptoServiceProvider keyCryptor = new RSACryptoServiceProvider(AsyncClient.CryptorKeySize);
        keyCryptor.ImportParameters(receivedContent.OpenKey);
        sendingContent.Key = keyCryptor.Encrypt(symmetricKey, false);
        keyCryptor.Clear();

        using (MemoryStream encryptedMessageStream = new MemoryStream(),
               messageStream = new MemoryStream(Encoding.Unicode.GetBytes(waitingMessage.Message)))
        {
          messageCrypter.EncryptStream(messageStream, encryptedMessageStream);
          sendingContent.Message = encryptedMessageStream.ToArray();
        }
      }

      ClientModel.Client.SendMessage(ServerSendPrivateMessageCommand.Id, sendingContent);
    }

    [Serializable]
    public class MessageContent
    {
      string nick;
      RSAParameters openKey;

      public string Nick { get { return nick; } set { nick = value; } }
      public RSAParameters OpenKey { get { return openKey; } set { openKey = value; } }
    }

    public const ushort Id = (ushort)ClientCommand.ReceiveUserOpenKey;
  }
}
