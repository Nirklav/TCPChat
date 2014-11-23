using Engine.Model.Common;
using Engine.Plugins;
using System;

namespace Engine.Model.Client
{
  public class ClientNotifier : Notifier<ClientNotifierContext>
  {
    internal void Connected(ConnectEventArgs args)
    {
      Notify((c, a) => c.OnConnected(a), args);
    }

    internal void ReceiveRegistrationResponse(RegistrationEventArgs args)
    {
      Notify((c, a) => c.OnReceiveRegistrationResponse(a), args);
    }

    internal void SystemMessage(string message)
    {
      Notify((c, a) => c.OnReceiveMessage(a), new ReceiveMessageEventArgs { Type = MessageType.System, Message = message });
    }

    internal void ReceiveMessage(ReceiveMessageEventArgs args)
    {
      Notify((c, a) => c.OnReceiveMessage(a), args);
    }

    internal void AsyncError(AsyncErrorEventArgs args)
    {
      Notify((c, a) => c.OnAsyncError(a), args);
    }

    internal void RoomRefreshed(RoomEventArgs args)
    {
      Notify((c, a) => c.OnRoomRefreshed(a), args);
    }

    internal void RoomOpened(RoomEventArgs args)
    {
      Notify((c, a) => c.OnRoomOpened(a), args);

    }
    internal void RoomClosed(RoomEventArgs args)
    {
      Notify((c, a) => c.OnRoomClosed(a), args);
    }

    internal void DownloadProgress(FileDownloadEventArgs args)
    {
      Notify((c, a) => c.OnDownloadProgress(a), args);
    }

    internal void PostedFileDeleted(FileDownloadEventArgs args)
    {
      Notify((c, a) => c.OnPostedFileDeleted(a), args);
    }

    internal void PluginLoaded(PluginEventArgs args)
    {
      Notify((c, a) => c.OnPluginLoaded(a), args);
    }

    internal void PluginUnloading(PluginEventArgs args)
    {
      Notify((c, a) => c.OnPluginUnloading(a), args);
    }

    protected override void Notify<TArgs>(Action<ClientNotifierContext, TArgs> methodInvoker, TArgs args)
    {
      base.Notify<TArgs>(methodInvoker, args);

      foreach (var context in ClientModel.Plugins.GetNotifierContexts())
        methodInvoker(context, args);
    }
  }

  public abstract class ClientNotifierContext : CrossDomainObject
  {
    /// <summary>
    /// Событие происходит при подключении клиента к серверу.
    /// </summary>
    protected internal virtual void OnConnected(ConnectEventArgs args) { }

    /// <summary>
    /// Событие происходит при полученни ответа от сервера, о регистрации.
    /// </summary>
    protected internal virtual void OnReceiveRegistrationResponse(RegistrationEventArgs args) { }

    /// <summary>
    /// Событие происходит при полученнии сообщения от сервера.
    /// </summary>
    protected internal virtual void OnReceiveMessage(ReceiveMessageEventArgs args) { }

    /// <summary>
    /// Событие происходит при любой асинхронной ошибке.
    /// </summary>
    protected internal virtual void OnAsyncError(AsyncErrorEventArgs args) { }

    /// <summary>
    /// Событие происходит при обновлении списка подключенных к серверу клиентов.
    /// </summary>
    protected internal virtual void OnRoomRefreshed(RoomEventArgs args) { }

    /// <summary>
    /// Событие происходит при открытии комнаты клиентом. Или когда клиента пригласили в комнату.
    /// </summary>
    protected internal virtual void OnRoomOpened(RoomEventArgs args) { }

    /// <summary>
    /// Событие происходит при закрытии комнаты клиентом, когда клиента кикают из комнаты.
    /// </summary>
    protected internal virtual void OnRoomClosed(RoomEventArgs args) { }

    /// <summary>
    /// Событие происходит при получении части файла, а также при завершении загрузки файла.
    /// </summary>
    protected internal virtual void OnDownloadProgress(FileDownloadEventArgs args) { }

    /// <summary>
    /// Происходит при удалении выложенного файла.
    /// </summary>
    protected internal virtual void OnPostedFileDeleted(FileDownloadEventArgs args) { }

    /// <summary>
    /// Происходит после успешной загрзуки плагина.
    /// </summary>
    protected internal virtual void OnPluginLoaded(PluginEventArgs args) { }

    /// <summary>
    /// Происходит перед выгрузкой плагина.
    /// </summary>
    protected internal virtual void OnPluginUnloading(PluginEventArgs args) { }
  }
}
