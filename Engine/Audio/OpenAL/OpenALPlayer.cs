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
  public sealed class OpenALPlayer :
    MarshalByRefObject,
    IPlayer
  {
    #region fields
    private readonly object _syncObject = new object();
    private bool _disposed;

    private AudioContext _context;
    private Dictionary<string, SourceDescription> _sources;
    #endregion

    #region nested types
    private class SourceDescription
    {
      public int Id { get; private set; }
      public long LastPlayedNumber { get; set; }

      [SecurityCritical]
      public SourceDescription(int soueceId)
      {
        Id = soueceId;
      }

      [SecurityCritical]
      public ALFormat GetFormat(SoundPack pack)
      {
        if (pack.Channels != 2 && pack.Channels != 1)
          throw new ArgumentException("channels");

        if (pack.BitPerChannel != 8 && pack.BitPerChannel != 16)
          throw new ArgumentException("bitPerChannel");

        if (pack.Channels == 1)
          return pack.BitPerChannel == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
        else
          return pack.BitPerChannel == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
      }
    }
    #endregion

    #region constructor
    [SecurityCritical]
    public OpenALPlayer(string deviceName = null)
    {
      if (string.IsNullOrEmpty(deviceName) || IsInited)
        return;

      Initialize(deviceName);
    }
    #endregion

    #region properties
    public bool IsInited
    {
      [SecuritySafeCritical]
      get { return Interlocked.CompareExchange(ref _context, null, null) != null; }
    }

    public IList<string> Devices
    {
      [SecuritySafeCritical]
      get
      {
        try
        {
          return AudioContext.AvailableDevices;
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
    private void Initialize(string deviceName)
    {
      try
      {
        lock (_syncObject)
        {
          _sources = new Dictionary<string, SourceDescription>();

          if (string.IsNullOrEmpty(deviceName))
            deviceName = AudioContext.DefaultDevice;

          if (!AudioContext.AvailableDevices.Contains(deviceName))
            deviceName = AudioContext.DefaultDevice;

          _context = new AudioContext(deviceName);
        }
      }
      catch (Exception e)
      {
        if (_context != null)
          _context.Dispose();

        _context = null;

        ClientModel.Logger.Write(e);
        throw new ModelException(ErrorCode.AudioNotEnabled, "Audio player do not initialized.", e, deviceName);
      }
    }

    [SecuritySafeCritical]
    public void SetOptions(string deviceName)
    {
      if (IsInited)
      {
        Stop();
        _context.Dispose();
      }

      Initialize(deviceName);
    }

    [SecuritySafeCritical]
    public void Enqueue(string id, long packNumber, SoundPack pack)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("id");

      if (!IsInited)
        return;

      lock (_syncObject)
      {
        SourceDescription source;
        if (!_sources.TryGetValue(id, out source))
        {
          int sourceId = AL.GenSource();
          source = new SourceDescription(sourceId);
          _sources.Add(id, source);
        }

        if (source.LastPlayedNumber > packNumber)
          return;

        source.LastPlayedNumber = packNumber;

        int bufferId = AL.GenBuffer();
        AL.BufferData(bufferId, source.GetFormat(pack), pack.Data, pack.Data.Length, pack.Frequency);
        AL.SourceQueueBuffer(source.Id, bufferId);

        if (AL.GetSourceState(source.Id) != ALSourceState.Playing)
          AL.SourcePlay(source.Id);

        ClearBuffers(source, 0);
      }
    }

    [SecuritySafeCritical]
    public void Stop(string id)
    {
      if (!IsInited)
        return;

      lock (_syncObject)
      {
        SourceDescription source;
        if (!_sources.TryGetValue(id, out source))
          return;

        Stop(source);
        _sources.Remove(id);
      }
    }

    [SecuritySafeCritical]
    public void Stop()
    {
      if (!IsInited)
        return;

      lock (_syncObject)
      {
        foreach (SourceDescription source in _sources.Values)
          Stop(source);

        _sources.Clear();
      }
    }

    [SecurityCritical]
    private void Stop(SourceDescription source)
    {
      int count;
      AL.GetSource(source.Id, ALGetSourcei.BuffersQueued, out count);
      ClearBuffers(source, count);

      AL.DeleteSource(source.Id);
    }

    [SecurityCritical]
    private void ClearBuffers(string id, int input)
    {
      SourceDescription source;
      if (!_sources.TryGetValue(id, out source))
        return;

      ClearBuffers(source, input);
    }

    [SecurityCritical]
    private void ClearBuffers(SourceDescription source, int count)
    {
      if (_context == null)
        return;

      int[] freedbuffers;
      if (count == 0)
      {
        int buffersProcessed;
        AL.GetSource(source.Id, ALGetSourcei.BuffersProcessed, out buffersProcessed);

        if (buffersProcessed == 0)
          return;

        freedbuffers = AL.SourceUnqueueBuffers(source.Id, buffersProcessed);
      }
      else
        freedbuffers = AL.SourceUnqueueBuffers(source.Id, count);

      AL.DeleteBuffers(freedbuffers);
    }
    #endregion

    #region IDisposable
    [SecuritySafeCritical]
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      if (_context != null)
      {
        Stop();
        _context.Dispose();
      }
    }
    #endregion
  }
}
