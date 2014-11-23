using Engine.Model.Entities;
using System;

namespace Engine
{
  [Serializable]
  public class FileDownloadEventArgs : EventArgs
  {
    public int Progress { get; set; }
    public FileDescription File { get; set; }
    public string RoomName { get; set; }
    public bool Canceled { get; set; }
  }
}
