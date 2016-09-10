using Engine.Api.Server;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Messages
{
  [Serializable]
  public class ClientSendMessageAction : IAction
  {
    private readonly string _roomName;
    private readonly long? _messageId;
    private readonly string _text;

    /// <summary>
    /// Send message.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="text">Message text.</param>
    [SecuritySafeCritical]
    public ClientSendMessageAction(string roomName, string text)
      : this(roomName, null, text)
    {

    }

    /// <summary>
    /// Edit message.
    /// </summary>
    /// <param name="roomName">Room name.</param>
    /// <param name="messageId">Message id which be edited.</param>
    /// <param name="text">Message text.</param>
    [SecuritySafeCritical]
    public ClientSendMessageAction(string roomName, long messageId, string text)
      : this(roomName, (long?)messageId, text)
    {

    }

    [SecuritySafeCritical]
    private ClientSendMessageAction(string roomName, long? messageId, string text)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(text))
        throw new ArgumentNullException("text");

      _roomName = roomName;
      _messageId = messageId;
      _text = text;
    }

    [SecuritySafeCritical]
    public void Pefrorm()
    {
      var sendingContent = new ServerSendRoomMessageCommand.MessageContent { RoomName = _roomName, MessageId = _messageId, Text = _text };
      ClientModel.Client.SendMessage(ServerSendRoomMessageCommand.CommandId, sendingContent);
    }
  }
}
