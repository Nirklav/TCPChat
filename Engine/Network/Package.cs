using System;
using System.Security;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Network
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
  [BinType("PackageDto")]
  [SecuritySafeCritical]
  public class Package : IPackage
  {
    [BinField("i")]
    private long _id;

    [SecuritySafeCritical]
    public Package(long id)
    {
      _id = id;
    }

    public long Id
    {
      get { return _id; }
    }
  }

  [Serializable]
  [BinType("PackageTDto")]
  [SecuritySafeCritical]
  public class Package<T> : Package, IPackage<T>
  {  
    [BinField("c")]
    private T _content;

    [SecuritySafeCritical]
    public Package(long id, T content)
      : base(id)
    {
      _content = content;
    }
  
    public T Content { get { return _content; } }
  }
}
