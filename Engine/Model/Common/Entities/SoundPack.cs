using System;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Entities
{
  /// <summary>
  /// Recorderd voice data.
  /// </summary>
  [Serializable]
  [BinType("SoundPackDto")]
  public class SoundPack
  {
    /// <summary>
    /// Recorded data.
    /// </summary>
    [BinField("d")]
    public byte[] Data;

    /// <summary>
    /// Channels count.
    /// </summary>
    [BinField("c")]
    public int Channels;

    /// <summary>
    /// Pits per channel count.
    /// </summary>
    [BinField("b")]
    public int BitPerChannel;

    /// <summary>
    /// Frequency.
    /// </summary>
    [BinField("f")]
    public int Frequency;
  } 
}
