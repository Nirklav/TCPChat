namespace Engine.Exceptions
{
  public enum ErrorCode
  {
    ApiNotSupported = 1,
    FileAlreadyDownloading = 2,
    FreePortDontFound = 3,
    LargeReceivedData = 4,
    CantDownloadOwnFile = 5,

    AudioNotEnabled = 100,

    PluginError = 200,

    RoomNotFound = 300,
    FileInRoomNotFound = 301,
    UnknownRoomType = 302,

    WrongContentType = 1000,
    IllegalInvoker = 1001
  }
}
