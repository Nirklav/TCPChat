using System;

namespace Engine.Model.Entities
{
  /// <summary>
  /// Класс описывающий файл раздаваемый пользователем.
  /// </summary>
  [Serializable]
  public class FileDescription : IEquatable<FileDescription>
  {
    private readonly string owner;
    private readonly int id;
    private readonly string name;
    private readonly long size;

    /// <summary>
    /// Создает новый экземпляр класса.
    /// </summary>
    /// <param name="fileOwner">Пользователь раздающий файл.</param>
    /// <param name="fileSize">Размер файла.</param>
    /// <param name="fileName">Короткое имя файла.</param>
    /// <param name="fileId">Индетификатор файла. В пределах пользователя должен быть уникален.</param>
    public FileDescription(string fileOwner, long fileSize, string fileName, int fileId)
    {
      owner = fileOwner;
      id = fileId;
      name = fileName;
      size = fileSize;
    }

    /// <summary>
    /// Пользователь раздающий файл.
    /// </summary>
    public string Owner
    {
      get { return owner; }
    }

    /// <summary>
    /// Идентификатор файла.
    /// </summary>
    public int Id
    {
      get { return id; }
    }

    /// <summary>
    /// Имя файла.
    /// </summary>
    public string Name
    {
      get { return name; }
    }

    /// <summary>
    /// Размер файла.
    /// </summary>
    public long Size
    {
      get { return size; }
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(obj, null))
        return false;

      if (ReferenceEquals(obj, this))
        return true;

      var file = obj as FileDescription;
      if (ReferenceEquals(file, null))
        return false;

      return Equals(file);
    }

    public bool Equals(FileDescription file)
    {
      if (ReferenceEquals(file, null))
        return false;

      if (ReferenceEquals(file, this))
        return true;

      return owner.Equals(file.owner) && id == file.id;
    }

    public override int GetHashCode()
    {
      return (owner.GetHashCode() * 397) ^ id.GetHashCode();
    }
  }
}
