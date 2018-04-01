using Engine.Model.Common.Dto;
using System;
using System.Security;

namespace Engine.Model.Common.Entities
{
  /// <summary>
  /// User message.
  /// </summary>
  [Serializable]
  public class Message
  {
    /// <summary>
    /// Message id.
    /// </summary>
    public readonly long Id;

    /// <summary>
    /// Message sender.
    /// </summary>
    public readonly UserId Owner;

    /// <summary>
    /// Time when message was sent.
    /// </summary>
    public readonly DateTime Time;

    /// <summary>
    /// Message text.
    /// </summary>
    public string Text;

    [SecuritySafeCritical]
    public Message(MessageDto dto)
      : this(dto.Id, dto.Owner, dto.Text, dto.Time)
    {

    }

    [SecuritySafeCritical]
    public Message(long id, UserId owner, string text)
      : this(id, owner, text, DateTime.UtcNow)
    {
    }

    [SecuritySafeCritical]
    public Message(long id, UserId owner, string text, DateTime time)
    {
      Owner = owner;
      Id = id;
      Text = text;
      Time = time;
    }

    /// <summary>
    /// Trying to concat other message with this.
    /// </summary>
    /// <param name="other">Other message which trying to contact with this.</param>
    /// <returns>If result is true then message was concated, otherwise - false.</returns>
    [SecuritySafeCritical]
    public bool TryConcat(Message other)
    {
      if (Owner != other.Owner)
        return false;

      const double ConcatMinutes = 1;
      if ((other.Time - Time).TotalMinutes > ConcatMinutes)
        return false;

      Text += string.Format("{0}{1}", Environment.NewLine, other.Text);
      return true;
    }

    [SecuritySafeCritical]
    public MessageDto ToDto()
    {
      return new MessageDto(this);
    }
  }
}
