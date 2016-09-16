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
    public long Id
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    /// <summary>
    /// Time when message was sent.
    /// </summary>
    public DateTime Time
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    /// <summary>
    /// Message text.
    /// </summary>
    public string Text
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      set;
    }

    /// <summary>
    /// Message sender.
    /// </summary>
    public string Owner
    {
      [SecuritySafeCritical]
      get;
      [SecuritySafeCritical]
      private set;
    }

    [SecuritySafeCritical]
    public Message(string owner, long id, string text)
      : this(owner, id, text, DateTime.UtcNow)
    {
    }

    [SecuritySafeCritical]
    public Message(string owner, long id, string text, DateTime time)
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
      if (Owner != null && !Owner.Equals(other.Owner))
        return false;

      const double ConcatMinutes = 1;
      if ((other.Time - Time).TotalMinutes > ConcatMinutes)
        return false;

      Text += string.Format("{0}{1}", Environment.NewLine, other.Text);
      return true;
    }

    [SecuritySafeCritical]
    public Message Clone()
    {
      return new Message(Owner, Id, Text, Time);
    }
  }
}
