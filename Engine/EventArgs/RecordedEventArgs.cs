using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{
  [Serializable]
  public class RecordedEventArgs : EventArgs
  {
    public RecordedEventArgs(byte[] data, int availableData, int channels, int bits, int frequency)
    {
      Data = data;
      DataSize = availableData * channels * (bits / 8);
      Channels = channels;
      BitPerChannel = bits;
      Frequency = frequency;
    }

    public byte[] Data { get; private set; }
    public int DataSize { get; private set; }
    public int Channels { get; private set; }
    public int BitPerChannel { get; private set; }
    public int Frequency { get; private set; }
  }
}
