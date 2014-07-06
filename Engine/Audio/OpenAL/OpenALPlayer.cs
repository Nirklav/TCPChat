using Engine.Model.Entities;
using OpenAL;
using System;
using System.Collections.Generic;

namespace Engine.Audio.OpenAL
{
  public class OpenALPlayer : IPlayer
  {
    #region fields
    private AudioContext context;
    private Dictionary<string, SourceDescription> sources;
    #endregion

    #region nested types
    private class SourceDescription
    {
      public int Id { get; private set; }
      public long LastPlayedNumber { get; set; }

      public SourceDescription(int soueceId)
      {
        Id = soueceId;
      }

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
    public OpenALPlayer(string deviceName = null)
    {
      if (string.Equals(deviceName, string.Empty))
        return;

      sources = new Dictionary<string, SourceDescription>();

      Initialize(deviceName);
    }
    #endregion

    #region methods
    private void Initialize(string deviceName)
    {
      if (deviceName == null)
        return;

      if (!AudioContext.AvailableDevices.Contains(deviceName))
        throw new ArgumentException("deviceName");

      context = new AudioContext(deviceName);
    }

    public void SetOptions(string deviceName)
    {
      if (context != null)
      {
        Stop();
        context.Dispose();
      }

      Initialize(deviceName);
    }

    public void Enqueue(string id, long packNumber, SoundPack pack)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("id");

      lock (sources)
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

    public void Stop(string id)
    {
      lock (sources)
      {
        SourceDescription source;
        if (!sources.TryGetValue(id, out source))
          return;

        Stop(source);
        sources.Remove(id);
      }
    }

    public void Stop()
    {
      lock (sources)
      {
        foreach (SourceDescription source in sources.Values)
          Stop(source);

        sources.Clear();
      }
    }

    private void Stop(SourceDescription source)
    {
      int count;
      AL.GetSource(source.Id, ALGetSourcei.BuffersQueued, out count);
      ClearBuffers(source, count);

      AL.DeleteSource(source.Id);
    }

    private void ClearBuffers(string id, int input)
    {
      SourceDescription source;
      if (!sources.TryGetValue(id, out source))
        return;

      ClearBuffers(source, input);
    }

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
    private bool disposed = false;

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
