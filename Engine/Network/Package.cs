using System;

namespace Engine.Network.Connections
{
  public interface IPackage
  {
    long Id { get; }
  }

  public interface IPackage<out T> : IPackage
  {
    T Content { get; }
  }

  [Serializable]
  public class Package : IPackage
  {
    private long id;

    public Package(long id)
    {
      this.id = id;
    }

    public long Id { get { return id; } }
  }

  [Serializable]
  public class Package<T> : Package, IPackage<T>
  {  
    private T content;

    public Package(long id, T content)
      : base(id)
    {
      this.content = content;
    }
  
    public T Content { get { return content; } }
  }
}
