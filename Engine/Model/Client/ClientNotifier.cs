using Engine.Model.Common;
using Engine.Plugins;
using System;
using System.Collections.Generic;

namespace Engine.Model.Client
{
  public class ClientNotifier : Notifier
  {
    public override object[] GetContexts()
    {
      var contexts = new List<object>(base.GetContexts());

      foreach (var context in ClientModel.Plugins.GetNotifierContexts())
        contexts.Add(context);

      return contexts.ToArray();
    }
  }

  [Notifier(typeof(IClientNotifierContext), BaseNotifier = typeof(ClientNotifier))]
  public interface IClientNotifier
  {
    void Connected(ConnectEventArgs args);
    void ReceiveRegistrationResponse(RegistrationEventArgs args);
    void ReceiveMessage(ReceiveMessageEventArgs args);
    void AsyncError(AsyncErrorEventArgs args);
    void RoomRefreshed(RoomEventArgs args);
    void RoomOpened(RoomEventArgs args);
    void RoomClosed(RoomEventArgs args);
    void DownloadProgress(FileDownloadEventArgs args);
    void PostedFileDeleted(FileDownloadEventArgs args);
    void PluginLoaded(PluginEventArgs args);
    void PluginUnloading(PluginEventArgs args);
  }

  public interface IClientNotifierContext
  {
    /// <summary>
    /// Событие происходит при подключении клиента к серверу.
    /// </summary>
    event EventHandler<ConnectEventArgs> Connected;

    /// <summary>
    /// Событие происходит при полученни ответа от сервера, о регистрации.
    /// </summary>
    event EventHandler<RegistrationEventArgs> ReceiveRegistrationResponse;

    /// <summary>
    /// Событие происходит при полученнии сообщения от сервера.
    /// </summary>
    event EventHandler<ReceiveMessageEventArgs> ReceiveMessage;

    /// <summary>
    /// Событие происходит при любой асинхронной ошибке.
    /// </summary>
    event EventHandler<AsyncErrorEventArgs> AsyncError;

    /// <summary>
    /// Событие происходит при обновлении списка подключенных к серверу клиентов.
    /// </summary>
    event EventHandler<RoomEventArgs> RoomRefreshed;

    /// <summary>
    /// Событие происходит при открытии комнаты клиентом. Или когда клиента пригласили в комнату.
    /// </summary>
    event EventHandler<RoomEventArgs> RoomOpened;

    /// <summary>
    /// Событие происходит при закрытии комнаты клиентом, когда клиента кикают из комнаты.
    /// </summary>
    event EventHandler<RoomEventArgs> RoomClosed;

    /// <summary>
    /// Событие происходит при получении части файла, а также при завершении загрузки файла.
    /// </summary>
    event EventHandler<FileDownloadEventArgs> DownloadProgress;

    /// <summary>
    /// Происходит при удалении выложенного файла.
    /// </summary>
    event EventHandler<FileDownloadEventArgs> PostedFileDeleted;

    /// <summary>
    /// Происходит после успешной загрзуки плагина.
    /// </summary>
    event EventHandler<PluginEventArgs> PluginLoaded;

    /// <summary>
    /// Происходит перед выгрузкой плагина.
    /// </summary>
    event EventHandler<PluginEventArgs> PluginUnloading;
  }
}
