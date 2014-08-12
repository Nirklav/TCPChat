using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Exceptions
{
  public enum ErrorCode
  {
    APINotSupported = 1,
    FileAlreadyDownloading = 2,
    FreePortDontFind = 3,
    LargeReceivedData = 4,

    AudioNotEnabled = 100,
  }
}
