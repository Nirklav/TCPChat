using Engine.Model.Common.Entities;
using System;

namespace Engine
{
  [Serializable]
  public class FileDownloadEventArgs : EventArgs
  {
    public string RoomName { get; set; }
    public FileId FileId { get; set; }

    public int Progress { get; set; }
    public bool Canceled { get; set; }
  }
}
