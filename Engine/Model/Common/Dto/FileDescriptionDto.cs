using Engine.Model.Common.Entities;
using System;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  /// <summary>
  /// File description data transfer object.
  /// </summary>
  [Serializable]
  [BinType("FileDescriptionDto")]
  public class FileDescriptionDto
  {
    [BinField("i")]
    public FileId Id;

    [BinField("s")]
    public long Size;

    [BinField("n")]
    public string Name;

    public FileDescriptionDto(FileDescription file)
      : this(file.Id, file.Size, file.Name)
    {

    }

    public FileDescriptionDto(FileId id, long size, string name)
    {
      Id = id;
      Size = size;
      Name = name;
    }
  }
}
