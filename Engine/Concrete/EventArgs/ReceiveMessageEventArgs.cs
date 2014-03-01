using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete
{
  public class ReceiveMessageEventArgs : EventArgs
  {
    public MessageType Type { get; set; }
    public string Sender { get; set; }
    public string Message { get; set; }
    public string RoomName { get; set; }
    public object State { get; set; }
  }

  public enum MessageType
  {
    Common,
    Private,
    System,
    File
  }
}
