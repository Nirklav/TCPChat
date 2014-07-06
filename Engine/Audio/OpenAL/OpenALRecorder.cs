using Engine.Model.Entities;
using OpenAL;
using System;
using System.Threading;

namespace Engine.Audio.OpenAL
{
  public class OpenALRecorder : IRecorder
  {
    #region consts
    private const int DefaultBufferSize = 4096;
    #endregion

    #region fields
    private object syncObj = new object();

    private Timer systemTimer;
    private AudioCapture capture;
    private byte[] buffer;

    private AudioQuality quality;
    private int samplesSize;
    #endregion

    #region event
    public event EventHandler<RecordedEventArgs> Recorded;
    #endregion

    #region constructor
    public OpenALRecorder(string deviceName = null)
    {
      if (string.IsNullOrEmpty(deviceName))
        return;

      if (!AudioCapture.AvailableDevices.Contains(deviceName))
        throw new ArgumentException("deviceName");

      if ((capture != null && capture.IsRunning) || string.Equals(deviceName, string.Empty))
        return;

      Initialize(deviceName, new AudioQuality(1, 16, 44100));
    }
    #endregion

    #region methods
    private void Initialize(string deviceName, AudioQuality quality)
    {
      this.quality = quality;
      this.samplesSize = DefaultBufferSize;

      ALFormat format;

      if (quality.Channels == 1)
        format = quality.Bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
      else
        format = quality.Bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;

      lock (syncObj)
      {
        buffer = new byte[quality.Channels * (quality.Bits / 8) * samplesSize * 2];
        capture = new AudioCapture(AudioCapture.DefaultDevice, quality.Frequency, format, samplesSize * 2);
      }
    }

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

    public void SetOptions(string deviceName, AudioQuality quality)
    {
      if ((capture != null && capture.IsRunning) || string.Equals(AudioCapture.DefaultDevice, string.Empty))
        throw new ArgumentException("recorder should be stopped");

      if (capture != null)
        capture.Dispose();

      Initialize(deviceName, quality);
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
          int availableDataSize = availableSamples * quality.Channels * (quality.Bits / 8);
          if (availableDataSize > buffer.Length)
            buffer = new byte[availableDataSize * 2];

          capture.ReadSamples(buffer, availableSamples);

          var temp = Interlocked.CompareExchange(ref Recorded, null, null);
          if (temp != null)
            temp(this, new RecordedEventArgs(buffer, availableSamples, quality.Channels, quality.Bits, quality.Frequency));
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
      double timeToBufferFilled = samplesSize / (quality.Frequency * 1000d);
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
