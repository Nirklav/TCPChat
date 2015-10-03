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
  public sealed class OpenALPlayer :
    MarshalByRefObject,
    IPlayer
  {
    #region fields
    private readonly object syncObject = new object();
    private bool disposed;

    private AudioContext context;
    private Dictionary<string, SourceDescription> sources;
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
      get { return Interlocked.CompareExchange(ref context, null, null) != null; }
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
        lock (syncObject)
        {
          sources = new Dictionary<string, SourceDescription>();

          if (string.IsNullOrEmpty(deviceName))
            deviceName = AudioContext.DefaultDevice;

          if (!AudioContext.AvailableDevices.Contains(deviceName))
            deviceName = AudioContext.DefaultDevice;

          context = new AudioContext(deviceName);
        }
      }
      catch (Exception e)
      {
        if (context != null)
          context.Dispose();

        context = null;

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
        context.Dispose();
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

      lock (syncObject)
      {
        SourceDescription source;
        if (!sources.TryGetValue(id, out source))
        {
          int sourceId = AL.GenSource();
          source = new SourceDescription(sourceId);
          sources.Add(id, source);
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

      lock (syncObject)
      {
        SourceDescription source;
        if (!sources.TryGetValue(id, out source))
          return;

        Stop(source);
        sources.Remove(id);
      }
    }

    [SecuritySafeCritical]
    public void Stop()
    {
      if (!IsInited)
        return;

      lock (syncObject)
      {
        foreach (SourceDescription source in sources.Values)
          Stop(source);

        sources.Clear();
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
      if (!sources.TryGetValue(id, out source))
        return;

      ClearBuffers(source, input);
    }

    [SecurityCritical]
    private void ClearBuffers(SourceDescription source, int count)
    {
      if (context == null)
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
      if (disposed)
        return;

      disposed = true;

      if (context != null)
      {
        Stop();
        context.Dispose();
      }
    }
    #endregion
  }
}
