namespace Engine.Exceptions
{
  public enum ErrorCode
  {
    APINotSupported = 1,
    FileAlreadyDownloading = 2,
    FreePortDontFind = 3,
    LargeReceivedData = 4,
    CantDownloadOwnFile = 5,

    AudioNotEnabled = 100,

    PluginError = 200,

    RoomNotFound = 300,
    FileInRoomNotFound = 301,

    WrongContentType = 1000,
    IllegalInvoker = 1001,
  }
}
