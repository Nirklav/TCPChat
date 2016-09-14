using Engine.Model.Common.Entities;
using System;

namespace Engine
{
  [Serializable]
  public class FileDownloadEventArgs : EventArgs
  {
    public string RoomName { get; private set; }
    public FileId FileId { get; private set; }

    public int Progress { get; private set; }

    public FileDownloadEventArgs(string roomName, FileId fileId, int progress)
    {
      RoomName = roomName;
      FileId = fileId;
      Progress = progress;
    }
  }
}
