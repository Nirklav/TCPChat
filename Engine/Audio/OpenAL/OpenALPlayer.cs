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
      public SourceDescription(int soueceId, int channels, int bitPerChannel, int frequency)
      {
        if (channels != 2 && channels != 1)
          throw new ArgumentException("channels");

        if (bitPerChannel != 8 && bitPerChannel != 16)
          throw new ArgumentException("bitPerChannel");

        if (frequency <= 0)
          throw new ArgumentException("frequency");

        if (channels == 1)
          Format = bitPerChannel == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
        else
          Format = bitPerChannel == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;

        Id = soueceId;
        Frequency = frequency;
      }

      public int Id { get; private set; }
      public ALFormat Format { get; private set; }
      public int Frequency { get; private set; }
      public long LastPlayedNumber { get; set; }
    }
    #endregion

    #region constructor
    public OpenALPlayer()
    {
      sources = new Dictionary<string, SourceDescription>();
      context = new AudioContext(AudioContext.DefaultDevice);
    }
    #endregion

    #region methods
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
          source = new SourceDescription(sourceId, pack.Channels, pack.BitPerChannel, pack.Frequency);
          sources.Add(id, source);
        }

        if (source.LastPlayedNumber > packNumber)
          return;

        source.LastPlayedNumber = packNumber;

        int bufferId = AL.GenBuffer();
        AL.BufferData(bufferId, source.Format, pack.Data, pack.Data.Length, source.Frequency);
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
      }
    }

    public void Stop()
    {
      lock (sources)
      {
        foreach (SourceDescription source in sources.Values)
          Stop(source);
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

      Stop();

      context.Dispose();
    }
    #endregion
  }
}
