using System;
using System.Threading;

namespace Engine.Model.Client
{
  public class ClientEventNotifierContext : ClientNotifierContext
  {
    public static event EventHandler<RoomEventArgs> RoomRefreshed;
    protected internal override void OnRoomRefreshed(RoomEventArgs args) { Raise(ref RoomRefreshed, args); }

    public static event EventHandler<ConnectEventArgs> Connected;
    protected internal override void OnConnected(ConnectEventArgs args) { Raise(ref Connected, args); }

    public static event EventHandler<RegistrationEventArgs> ReceiveRegistrationResponse;
    protected internal override void OnReceiveRegistrationResponse(RegistrationEventArgs args) { Raise(ref ReceiveRegistrationResponse, args); }

    public static event EventHandler<ReceiveMessageEventArgs> ReceiveMessage;
    protected internal override void OnReceiveMessage(ReceiveMessageEventArgs args) { Raise(ref ReceiveMessage, args); }

    public static event EventHandler<AsyncErrorEventArgs> AsyncError;
    protected internal override void OnAsyncError(AsyncErrorEventArgs args) { Raise(ref AsyncError, args); }

    public static event EventHandler<RoomEventArgs> RoomOpened;
    protected internal override void OnRoomOpened(RoomEventArgs args) { Raise(ref RoomOpened, args); }

    public static event EventHandler<RoomEventArgs> RoomClosed;
    protected internal override void OnRoomClosed(RoomEventArgs args) { Raise(ref RoomClosed, args); }

    public static event EventHandler<FileDownloadEventArgs> DownloadProgress;
    protected internal override void OnDownloadProgress(FileDownloadEventArgs args) { Raise(ref DownloadProgress, args); }

    public static event EventHandler<FileDownloadEventArgs> PostedFileDeleted;
    protected internal override void OnPostedFileDeleted(FileDownloadEventArgs args) { Raise(ref PostedFileDeleted, args); }

    public static event EventHandler<PluginEventArgs> PluginLoaded;
    protected internal override void OnPluginLoaded(PluginEventArgs args) { Raise(ref PluginLoaded, args); }

    public static event EventHandler<PluginEventArgs> PluginUnloading;
    protected internal override void OnPluginUnloading(PluginEventArgs args) { Raise(ref PluginUnloading, args); }

    private static void Raise<T>(ref EventHandler<T> eventHandler, T args) where T : EventArgs
    {
      var temp = Interlocked.CompareExchange(ref eventHandler, null, null);
      if (temp != null)
        temp(null, args);
    }
  }
}
