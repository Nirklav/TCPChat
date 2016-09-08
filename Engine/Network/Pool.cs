using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;

namespace Engine.Network
{
  public class Pool
  {
    private const int DefaultSize = 2048;
    
    public struct Guard : IDisposable
    {
      private readonly Pool _owner;
      private readonly MemoryStream _stream;

      public MemoryStream Stream { get { return _stream; } }

      [SecurityCritical]
      public Guard(Pool guardOwner, MemoryStream memoryStream)
      {
        _owner = guardOwner;
        _stream = memoryStream;
      }

      [SecuritySafeCritical]
      public void Dispose()
      {
        _owner.Put(Stream);
      }
    }

    private struct Container : IEquatable<Container>
    {
      public readonly MemoryStream Data;
      public readonly int Size;

      [SecurityCritical]
      public Container(MemoryStream data)
      {
        Data = data;
        Size = data.Capacity;
      }

      [SecurityCritical]
      public Container(int size)
      {
        Data = null;
        Size = size;
      }

      [SecuritySafeCritical]
      public override bool Equals(object obj)
      {
        if (ReferenceEquals(obj, null))
          return false;

        if (!(obj is Container))
          return false;

        return Equals((Container)obj);
      }

      [SecuritySafeCritical]
      public bool Equals(Container other)
      {
        return Size == other.Size;
      }

      [SecuritySafeCritical]
      public override int GetHashCode()
      {
        return Size;
      }
    }

    private class Comparer : IComparer<Container>
    {
      [SecuritySafeCritical]
      public int Compare(Container x, Container y)
      {
        return x.Size.CompareTo(y.Size);
      }
    }

    private static long Misses;
    private static long Hits;
    private static long Puts;

    private readonly int _maxSize;
    private readonly List<Container> _storage;
    private readonly Comparer _comaprer;
    
    [SecurityCritical]
    public Pool(int maxSize)
    {
      _maxSize = maxSize;
      _storage = new List<Container>(_maxSize);
      _comaprer = new Comparer();
    }
    
    [SecurityCritical]
    public Guard GetWithGuard(int? size = null)
    {
      return new Guard(this, Get(size));
    }
    
    [SecurityCritical]
    public MemoryStream Get(int? size = null)
    {
      lock (_storage)
      {
        var streamSize = size ?? DefaultSize;
        if (streamSize <= 0)
          throw new ArgumentException("size");

        var index = _storage.BinarySearch(new Container(streamSize), _comaprer);
        if (index < 0)
        {
          index = ~index;

          if (index >= _storage.Count)
          {
            if (size == null && _storage.Count >= 1)
              index = 0;
            else
            {
              Interlocked.Increment(ref Misses);
              return new MemoryStream(streamSize);
            }
          }
        }
        
        var result = _storage[index];
        _storage.RemoveAt(index);

        Interlocked.Increment(ref Hits);
        return result.Data;
      }
    }
    
    [SecurityCritical]
    public void Put(MemoryStream data)
    {
      Interlocked.Increment(ref Puts);

      lock (_storage)
      {
        if (_storage.Count < _maxSize)
        {
          data.Position = 0;
          data.SetLength(0);

          var container = new Container(data);
          var index = _storage.BinarySearch(container, _comaprer);
          if (index >= 0)
            _storage.Insert(index, container);
          else
            _storage.Insert(~index, container);
        }
      }
    }
  }
}
