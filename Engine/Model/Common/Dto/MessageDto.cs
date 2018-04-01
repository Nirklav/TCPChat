using Engine.Model.Common.Entities;
using System;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// Message data transfer object.
  /// </summary>
  [Serializable]
  [BinType("MessageDto")]
  public class MessageDto
  {
    [BinField("i")]
    public long Id;

    [BinField("t")]
    public DateTime Time;

    [BinField("x")]
    public string Text;

    [BinField("o")]
    public UserId Owner;

    public MessageDto(Message msg)
    {
      Id = msg.Id;
      Time = msg.Time;
      Text = msg.Text;
      Owner = msg.Owner;
    }
  }
}
