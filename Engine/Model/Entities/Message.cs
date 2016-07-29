using System;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Сообщение пользователя.
  /// </summary>
  [Serializable]
  public class Message
  {
    /// <summary>
    /// Идетификатор сообщения.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Время отправки.
    /// </summary>
    public DateTime Time { get; private set; }

    /// <summary>
    /// Текст.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Ник пользователя отправившего сообщение.
    /// </summary>
    public string Owner { get; private set; }

    public Message(string owner, long id, string text)
      : this(owner, id, text, DateTime.UtcNow)
    {
    }

    public Message(string owner, long id, string text, DateTime time)
    {
      Owner = owner;
      Id = id;
      Text = text;
      Time = time;
    }

    /// <summary>
    /// Пытается соединенить сообщение.
    /// </summary>
    /// <param name="other">Другое сообщение.</param>
    /// <returns>Получилось ли соединить сообщение.</returns>
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
  }
}
