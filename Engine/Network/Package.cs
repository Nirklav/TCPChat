using System;

namespace Engine.Network
{
  public interface IPackage
  {
    long Id { get; }
    long TrackedId { get; set; }
  }

  public interface IPackage<out T> : IPackage
  {
    T Content { get; }
  }

  [Serializable]
  public class Package : IPackage
  {
    private long _id;
    private long _trackedId;

    public Package(long id)
    {
      _id = id;
    }

    public long Id { get { return _id; } }
    public long TrackedId
    {
      get { return _trackedId; }
      set { _trackedId = value; }
    }
  }

  [Serializable]
  public class Package<T> : Package, IPackage<T>
  {  
    private T _content;

    public Package(long id, T content)
      : base(id)
    {
      _content = content;
    }
  
    public T Content { get { return _content; } }
  }
}
