using System;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// Represents nullable type for BinarySerializer
  /// </summary>
  [Serializable]
  [BinType("BinNullable")]
  public sealed class BinNullable<T>
    where T: struct
  {
    [BinField("v")]
    public T Value;

    public BinNullable(T value)
    {
      Value = value;
    }

    public static implicit operator T?(BinNullable<T> nullable)
    {
      return nullable == null ? (T?)null : nullable.Value;
    }

    public static implicit operator BinNullable<T>(T value)
    {
      return new BinNullable<T>(value);
    }

    public static implicit operator BinNullable<T>(T? value)
    {
      if (value == null)
        return null;
      return new BinNullable<T>(value.Value);
    }
  }
}
