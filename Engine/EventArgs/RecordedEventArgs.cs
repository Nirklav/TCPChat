using System;

namespace Engine
{
  [Serializable]
  public class RecordedEventArgs : EventArgs
  {
    public byte[] Data { get; private set; }
    public int DataSize { get; private set; }
    public int Channels { get; private set; }
    public int BitPerChannel { get; private set; }
    public int Frequency { get; private set; }

    public RecordedEventArgs(byte[] data, int availableData, int channels, int bits, int frequency)
    {
      Data = data;
      DataSize = availableData * channels * (bits / 8);
      Channels = channels;
      BitPerChannel = bits;
      Frequency = frequency;
    }
  }
}
