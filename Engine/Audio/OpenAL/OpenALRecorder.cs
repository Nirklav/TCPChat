using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Entities;
using OpenAL;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;

namespace Engine.Audio.OpenAL
{
  public sealed class OpenALRecorder :
    MarshalByRefObject,
    IRecorder
  {
    #region consts
    private const int DefaultBufferSize = 1024 * 2;
    #endregion

    #region fields
    private readonly object syncObj = new object();
    private EventHandler<RecordedEventArgs> recorded;

    private bool disposed;    
    private AudioCapture capture;
    private byte[] buffer;
    private Timer captureTimer;
    private AudioQuality quality;
    private int samplesSize;
    #endregion

    #region event
    public event EventHandler<RecordedEventArgs> Recorded
    {
      [SecuritySafeCritical]
      add { recorded += value; }
      [SecuritySafeCritical]
      remove { recorded -= value; }
    }
    #endregion

    #region constructor
    [SecurityCritical]
    public OpenALRecorder(string deviceName = null)
    {
      if (string.IsNullOrEmpty(deviceName) || IsInited)
        return;

      Initialize(deviceName, new AudioQuality(1, 16, 44100));
    }
    #endregion

    #region properties
    public bool IsInited
    {
      [SecuritySafeCritical]
      get { return Interlocked.CompareExchange(ref capture, null, null) != null; }
    }

    public IList<string> Devices
    {
      [SecuritySafeCritical]
      get
      {
        try
        {
          return AudioCapture.AvailableDevices;
        }
        catch (Exception)
        {
          return new List<string>();
        }
      }
    }
    #endregion

    #region methods
    [SecurityCritical]
    private void Initialize(string deviceName, AudioQuality quality)
    {
      try
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

          if (string.IsNullOrEmpty(deviceName))
            deviceName = AudioCapture.DefaultDevice;

          if (!AudioCapture.AvailableDevices.Contains(deviceName))
            deviceName = AudioCapture.DefaultDevice;

          capture = new AudioCapture(deviceName, quality.Frequency, format, samplesSize);
        }
      }
      catch (Exception e)
      {
        if (capture != null)
          capture.Dispose();

        capture = null;

        ClientModel.Logger.Write(e);
        throw new ModelException(ErrorCode.AudioNotEnabled, "Audio recorder do not initialized.", e, deviceName);
      }
    }

    [SecuritySafeCritical]
    public void Start()
    {
      if (capture == null || capture.IsRunning)
        return;

      lock (syncObj)
      {
        capture.Start();
        if (captureTimer == null)
          captureTimer = new Timer(OnRecording, null, GetTimerTimeOut(), -1);
      }
    }

    [SecuritySafeCritical]
    public void SetOptions(string deviceName, AudioQuality quality)
    {
      if (IsInited)
      {
        Stop();
        capture.Dispose();
      }

      Initialize(deviceName, quality);
    }

    [SecurityCritical]
    private void OnRecording(object state)
    {
      lock (syncObj)
      {
        if (capture == null)
          return;

        var availableSamples = capture.AvailableSamples;
        if (availableSamples > 0)
        {
          var availableDataSize = availableSamples * quality.Channels * (quality.Bits / 8);
          if (availableDataSize > buffer.Length)
            buffer = new byte[availableDataSize * 2];

          capture.ReadSamples(buffer, availableSamples);

          var temp = Interlocked.CompareExchange(ref recorded, null, null);
          if (temp != null)
            temp(this, new RecordedEventArgs(buffer, availableSamples, quality.Channels, quality.Bits, quality.Frequency));
        }

        if (capture.IsRunning)
        {
          captureTimer.Change(GetTimerTimeOut(), -1);
        }
        else
        {
          captureTimer.Dispose();
          captureTimer = null;
        }
      }
    }

    [SecuritySafeCritical]
    public void Stop()
    {
      if (!IsInited)
        return;

      lock (syncObj)
      {
        capture.Stop();
      }
    }

    [SecurityCritical]
    private int GetTimerTimeOut()
    {
      var bufferFillMs = (double)samplesSize * 1000 / quality.Frequency;
      return (int)(bufferFillMs * 0.75d);
    }
    #endregion

    #region IDisposable
    [SecuritySafeCritical]
    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
      recorded = null;

      lock (syncObj)
      {
        if (captureTimer != null)
          captureTimer.Dispose();

        captureTimer = null;

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
