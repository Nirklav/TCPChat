using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class FileDownloadEventArgs : EventArgs
  {
    public int Progress { get; set; }
    public FileDescription File { get; set; }
    public string RoomName { get; set; }
    public bool Canceled { get; set; }
  }
}
