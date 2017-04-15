using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
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
    private readonly object _syncObj = new object();
    private EventHandler<RecordedEventArgs> _recorded;

    private bool _disposed;    
    private AudioCapture _capture;
    private byte[] _buffer;
    private Timer _captureTimer;
    private AudioQuality _quality;
    private int _samplesSize;
    #endregion

    #region event
    public event EventHandler<RecordedEventArgs> Recorded
    {
      [SecuritySafeCritical]
      add { _recorded += value; }
      [SecuritySafeCritical]
      remove { _recorded -= value; }
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
      get { return Interlocked.CompareExchange(ref _capture, null, null) != null; }
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
        _quality = quality;
        _samplesSize = DefaultBufferSize;

        ALFormat format;

        if (quality.Channels == 1)
          format = quality.Bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
        else
          format = quality.Bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;

        lock (_syncObj)
        {
          _buffer = new byte[quality.Channels * (quality.Bits / 8) * _samplesSize * 2];

          if (string.IsNullOrEmpty(deviceName))
            deviceName = AudioCapture.DefaultDevice;

          if (!AudioCapture.AvailableDevices.Contains(deviceName))
            deviceName = AudioCapture.DefaultDevice;

          _capture = new AudioCapture(deviceName, quality.Frequency, format, _samplesSize);
        }
      }
      catch (Exception e)
      {
        if (_capture != null)
          _capture.Dispose();

        _capture = null;

        ClientModel.Logger.Write(e);
        throw new ModelException(ErrorCode.AudioNotEnabled, "Audio recorder do not initialized.", e, deviceName);
      }
    }

    [SecuritySafeCritical]
    public void Start()
    {
      if (_capture == null || _capture.IsRunning)
        return;

      lock (_syncObj)
      {
        _capture.Start();
        if (_captureTimer == null)
          _captureTimer = new Timer(OnRecording, null, GetTimerTimeOut(), -1);
      }
    }

    [SecuritySafeCritical]
    public void SetOptions(string deviceName, AudioQuality quality)
    {
      if (IsInited)
      {
        Stop();
        _capture.Dispose();
      }

      Initialize(deviceName, quality);
    }

    [SecurityCritical]
    private void OnRecording(object state)
    {
      lock (_syncObj)
      {
        if (_capture == null)
          return;

        var availableSamples = _capture.AvailableSamples;
        if (availableSamples > 0)
        {
          var availableDataSize = availableSamples * _quality.Channels * (_quality.Bits / 8);
          if (availableDataSize > _buffer.Length)
            _buffer = new byte[availableDataSize * 2];

          _capture.ReadSamples(_buffer, availableSamples);

          var temp = Interlocked.CompareExchange(ref _recorded, null, null);
          if (temp != null)
            temp(this, new RecordedEventArgs(_buffer, availableSamples, _quality.Channels, _quality.Bits, _quality.Frequency));
        }

        if (_capture.IsRunning)
        {
          _captureTimer.Change(GetTimerTimeOut(), -1);
        }
        else
        {
          _captureTimer.Dispose();
          _captureTimer = null;
        }
      }
    }

    [SecuritySafeCritical]
    public void Stop()
    {
      if (!IsInited)
        return;

      lock (_syncObj)
      {
        _capture.Stop();
      }
    }

    [SecurityCritical]
    private int GetTimerTimeOut()
    {
      var bufferFillMs = (double)_samplesSize * 1000 / _quality.Frequency;
      return (int)(bufferFillMs * 0.75d);
    }
    #endregion

    #region IDisposable
    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      _recorded = null;

      lock (_syncObj)
      {
        if (_captureTimer != null)
          _captureTimer.Dispose();

        _captureTimer = null;

        if (_capture != null)
        {
          _capture.Stop();
          _capture.Dispose();
        }

        _capture = null;
      }
    }
    #endregion
  }
}
