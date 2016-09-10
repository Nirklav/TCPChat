using System;

namespace Engine.Model.Common.Entities
{
  /// <summary>
  /// Recorderd voice data.
  /// </summary>
  [Serializable]
  public class SoundPack
  {
    /// <summary>
    /// Recorded data.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Channels count.
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Pits per channel count.
    /// </summary>
    public int BitPerChannel { get; set; }

    /// <summary>
    /// Frequency.
    /// </summary>
    public int Frequency { get; set; }
  } 
}
