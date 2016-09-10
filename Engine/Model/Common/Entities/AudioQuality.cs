using System;

namespace Engine.Model.Common.Entities
{
  [Serializable]
  public class AudioQuality : IEquatable<AudioQuality>
  {
    public int Channels { get; set; }
    public int Bits { get; set; }
    public int Frequency { get; set; }

    public AudioQuality(int channels, int bits, int frequency)
    {
      if (channels != 2 && channels != 1)
        throw new ArgumentException("channels");

      if (bits != 8 && bits != 16)
        throw new ArgumentException("bitPerChannel");

      if (frequency <= 0)
        throw new ArgumentException("frequency");

      Channels = channels;
      Bits = bits;
      Frequency = frequency;
    }

    public override string ToString()
    {
      return string.Format("{0} бит / {1} Гц", Bits, Frequency);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;

      if (ReferenceEquals(obj, this))
        return true;

      var quality = obj as AudioQuality;
      if (ReferenceEquals(quality, null))
        return false;

      return Equals(quality);
    }

    public bool Equals(AudioQuality other)
    {
      if (ReferenceEquals(other, null))
        return false;

      if (ReferenceEquals(other, this))
        return true;

      return Channels == other.Channels &&
        Bits == other.Bits &&
        Frequency == other.Frequency;
    }

    public override int GetHashCode()
    {
      int hashCode = Channels;
      hashCode = (hashCode ^ 397) * Bits;
      hashCode = (hashCode ^ 397) * Frequency;
      return hashCode;
    }
  }
}
