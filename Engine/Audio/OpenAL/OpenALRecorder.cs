using OpenAL;
using System;
using System.Threading;

namespace Engine.Audio.OpenAL
{
  public class OpenALRecorder : IRecorder
  {
    #region fields
    private object syncObj = new object();

    private Timer systemTimer;
    private AudioCapture capture;
    private byte[] buffer;

    private int channels;
    private int bitPerChannel;
    private int frequency;
    private int samplesSize;
    #endregion

    #region event
    public event EventHandler<RecordedEventArgs> Recorded;
    #endregion

    #region constructor
    public OpenALRecorder(int channels, int bitPerChannel, int frequency, int samplesSize)
    {
      if ((capture != null && capture.IsRunning) || string.Equals(AudioCapture.DefaultDevice, string.Empty))
        return;

      if (channels != 2 && channels != 1)
        throw new ArgumentException("channels");

      if (bitPerChannel != 8 && bitPerChannel != 16)
        throw new ArgumentException("bitPerChannel");

      if (frequency <= 0)
        throw new ArgumentException("frequency");

      if (samplesSize <= 0)
        throw new ArgumentException("samplesBufferSize");

      this.channels = channels;
      this.bitPerChannel = bitPerChannel;
      this.frequency = frequency;
      this.samplesSize = samplesSize;

      ALFormat format;

      if (channels == 1)
        format = bitPerChannel == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
      else
        format = bitPerChannel == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;

      lock (syncObj)
      {
        buffer = new byte[channels * (bitPerChannel / 8) * samplesSize * 2];
        capture = new AudioCapture(AudioCapture.DefaultDevice, frequency, format, samplesSize * 2);
      }
    }
    #endregion

    #region methods
    public void Start()
    {
      if ((capture != null && capture.IsRunning) || string.Equals(AudioCapture.DefaultDevice, string.Empty))
        return;

      lock (syncObj)
      {
        capture.Start();
        systemTimer = new Timer(RecordingCallback, null, GetTimerTimeOut(), -1);
      }
    }

    public void SetOptions(int channels, int bitPerChannel, int frequency, int samplesSize)
    {
      throw new NotSupportedException("in this version - method do not support");
    }

    private void RecordingCallback(object state)
    {
      lock(syncObj)
      {
        if (capture == null || !capture.IsRunning || string.Equals(AudioCapture.DefaultDevice, string.Empty))
          return;

        int availableSamples = capture.AvailableSamples;

        if (availableSamples > 0)
        {
          int availableDataSize = availableSamples * channels * (bitPerChannel / 8);
          if (availableDataSize > buffer.Length)
            buffer = new byte[availableDataSize * 2];

          capture.ReadSamples(buffer, availableSamples);

          var temp = Interlocked.CompareExchange(ref Recorded, null, null);
          if (temp != null)
            temp(this, new RecordedEventArgs(buffer, availableSamples, channels, bitPerChannel, frequency));
        }

        if (systemTimer != null && capture.IsRunning)
          systemTimer.Change(GetTimerTimeOut(), -1);
      }
    }

    public void Stop()
    {
      if (capture == null || !capture.IsRunning || string.Equals(AudioCapture.DefaultDevice, string.Empty))
        return;

      lock (syncObj)
      {
        capture.Stop();

        if (systemTimer != null)
          systemTimer.Dispose();
        systemTimer = null;
      }
    }

    private int GetTimerTimeOut()
    {
      double timeToBufferFilled = samplesSize / (frequency * 1000d);
      return (int)(timeToBufferFilled / 2);
    }
    #endregion

    #region IDisposable
    private bool disposed = false;

    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
      Recorded = null;

      lock (syncObj)
      {
        if (systemTimer != null)
          systemTimer.Dispose();

        systemTimer = null;

        if (capture != null)
        {
          capture.Stop();
          capture.Dispose();
        }

        capture = null;
      }
    }
    #endregion
  }
}
