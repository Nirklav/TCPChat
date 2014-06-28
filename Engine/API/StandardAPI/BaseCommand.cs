using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Engine.API.StandardAPI
{
  abstract class BaseCommand
  {
    protected static T GetContentFormMessage<T>(byte[] message)
    {
      using (MemoryStream messageStream = new MemoryStream(message))
      {
        messageStream.Position = sizeof(ushort);
        BinaryFormatter formatter = new BinaryFormatter();
        T receivedContent = (T)formatter.Deserialize(messageStream);
        return receivedContent;
      }
    }
  }
}
