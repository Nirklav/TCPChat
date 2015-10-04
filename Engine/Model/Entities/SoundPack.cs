using System;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий кусок звуковых данных.
  /// </summary>
  [Serializable]
  public class SoundPack
  {
    /// <summary>
    /// Данные.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Количество каналов.
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Количество бит на канал.
    /// </summary>
    public int BitPerChannel { get; set; }

    /// <summary>
    /// Частота.
    /// </summary>
    public int Frequency { get; set; }
  } 
}
