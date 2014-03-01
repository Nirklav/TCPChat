using Engine.Abstract;
using Engine.Concrete.Containers;
using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Engine.Concrete.API.StandartAPI
{
  /// <summary>
  /// Класс реализующий стандартный API для клиента.
  /// </summary>
  class StandartClientAPI : IClientAPI
  {
    private Dictionary<ushort, IClientAPICommand> commandDictionary;
    private Random idCreator;

    /// <summary>
    /// Клинет являющийся хозяином данного API.
    /// </summary>
    public AsyncClient Client { get; private set; }

    /// <summary>
    /// Приватные сообщения которые ожидают открытого ключа, для шифрования.
    /// </summary>
    public List<WaitingPrivateMessage> WaitingPrivateMessages { get; private set; }

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    /// <param name="host">Клиент которому будет принадлежать данный API.</param>
    public StandartClientAPI(AsyncClient host)
    {
      if (host == null)
        throw new ArgumentNullException("host");

      commandDictionary = new Dictionary<ushort, IClientAPICommand>();
      WaitingPrivateMessages = new List<WaitingPrivateMessage>();
      idCreator = new Random(DateTime.Now.Millisecond);
      Client = host;

      commandDictionary.Add(ClientRegistrationResponseCommand.Id, new ClientRegistrationResponseCommand());
      commandDictionary.Add(ClientRoomRefreshedCommand.Id, new ClientRoomRefreshedCommand());
      commandDictionary.Add(ClientOutRoomMessageCommand.Id, new ClientOutRoomMessageCommand());
      commandDictionary.Add(ClientOutPrivateMessageCommand.Id, new ClientOutPrivateMessageCommand());
      commandDictionary.Add(ClientOutSystemMessageCommand.Id, new ClientOutSystemMessageCommand());
      commandDictionary.Add(ClientFilePostedCommand.Id, new ClientFilePostedCommand());
      commandDictionary.Add(ClientReceiveUserOpenKeyCommand.Id, new ClientReceiveUserOpenKeyCommand());
      commandDictionary.Add(ClientRoomOpenedCommand.Id, new ClientRoomOpenedCommand());
      commandDictionary.Add(ClientRoomClosedCommand.Id, new ClientRoomClosedCommand());
      commandDictionary.Add(ClientPostedFileDeletedCommand.Id, new ClientPostedFileDeletedCommand());
      commandDictionary.Add(ClientReadFilePartCommand.Id, new ClientReadFilePartCommand());
      commandDictionary.Add(ClientWriteFilePartCommand.Id, new ClientWriteFilePartCommand());
      commandDictionary.Add(ClientPingResponceCommand.Id, new ClientPingResponceCommand());
      commandDictionary.Add(ClientConnectToPeerCommand.Id, new ClientConnectToPeerCommand());
      commandDictionary.Add(ClientWaitPeerConnectionCommand.Id, new ClientWaitPeerConnectionCommand());
      commandDictionary.Add(ClientConnectToP2PServiceCommand.Id, new ClientConnectToP2PServiceCommand());
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
    /// <returns>Команда для выполнения.</returns>
    public IClientAPICommand GetCommand(byte[] message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (message.Length < 2)
        throw new ArgumentException("message.Length < 2");

      ushort id = BitConverter.ToUInt16(message, 0);

      try
      {
        return commandDictionary[id];
      }
      catch (KeyNotFoundException)
      {
        return ClientEmptyCommand.Empty;
      }
    }

    /// <summary>
    /// Асинхронно отправляет сообщение всем пользователям в комнате. Если клиента нет в комнате, сообщение игнорируется сервером.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public void SendMessage(string message, string roomName)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      ServerSendRoomMessageCommand.MessageContent sendingContent = new ServerSendRoomMessageCommand.MessageContent();
      sendingContent.Message = message;
      sendingContent.RoomName = roomName;
      Client.SendMessage(ServerSendRoomMessageCommand.Id, sendingContent);
    }

    /// <summary>
    /// Асинхронно отправляет сообщение конкретному пользователю.
    /// </summary>
    /// <param name="receiver">Ник получателя.</param>
    /// <param name="message">Сообщение.</param>
    public void SendPrivateMessage(string receiver, string message)
    {
      if (message == null)
        throw new ArgumentNullException("message");

      if (string.IsNullOrEmpty(receiver))
        throw new ArgumentException("receiver");

      lock (WaitingPrivateMessages)
        WaitingPrivateMessages.Add(new WaitingPrivateMessage() { Receiver = receiver, Message = message });

      ServerSendUserOpenKeyCommand.MessageContent sendingContent = new ServerSendUserOpenKeyCommand.MessageContent();
      sendingContent.Nick = receiver;
      Client.SendMessage(ServerSendUserOpenKeyCommand.Id, sendingContent);
    }

    /// <summary>
    /// Асинхронно послыает запрос для регистрации на сервере.
    /// </summary>
    /// <param name="info">Информация о юзере, по которой будет совершена попытка подключения.</param>
    /// <param name="keyCryptor">Откртый ключ клиента.</param>
    public void SendRegisterRequest(User info, RSAParameters openKey)
    {
      if (info == null)
        throw new ArgumentNullException("info");

      ServerRegisterCommand.MessageContent sendingContent = new ServerRegisterCommand.MessageContent();
      sendingContent.User = info;
      sendingContent.OpenKey = openKey;
      Client.SendMessage(ServerRegisterCommand.Id, sendingContent);
    }

    /// <summary>
    /// Асинхронно посылает запрос для отмены регистрации на сервере.
    /// </summary>
    public void SendUnregisterRequest()
    {
      Client.SendMessage(ServerUnregisterCommand.Id, null);
    }

    /// <summary>
    /// Создает на сервере комнату.
    /// </summary>
    /// <param name="roomName">Название комнаты для создания.</param>
    public void CreateRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      ServerCreateRoomCommand.MessageContent sendingContent = new ServerCreateRoomCommand.MessageContent() { RoomName = roomName };
      Client.SendMessage(ServerCreateRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Удаляет комнату на сервере. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    public void DeleteRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      ServerDeleteRoomCommand.MessageContent sendingContent = new ServerDeleteRoomCommand.MessageContent() { RoomName = roomName };
      Client.SendMessage(ServerDeleteRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Приглашает в комнату пользователей. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут приглашены.</param>
    public void InviteUsers(string roomName, IEnumerable<User> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      ServerInviteUsersCommand.MessageContent sendingContent = new ServerInviteUsersCommand.MessageContent() { RoomName = roomName, Users = users };
      Client.SendMessage(ServerInviteUsersCommand.Id, sendingContent);
    }

    /// <summary>
    /// Удаляет пользователей из комнаты. Необходимо являться создателем комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="users">Перечисление пользователей, которые будут удалены из комнаты.</param>
    public void KickUsers(string roomName, IEnumerable<User> users)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (users == null)
        throw new ArgumentNullException("users");

      ServerKickUsersCommand.MessageContent sendingContent = new ServerKickUsersCommand.MessageContent() { RoomName = roomName, Users = users };
      Client.SendMessage(ServerKickUsersCommand.Id, sendingContent);
    }

    /// <summary>
    /// Осуществляет выход из комнаты пользователя.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    public void ExitFormRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      ServerExitFormRoomCommand.MessageContent sendingContent = new ServerExitFormRoomCommand.MessageContent() { RoomName = roomName };
      Client.SendMessage(ServerExitFormRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Отправляет запрос о необходимости получения списка пользователей комнаты.
    /// </summary>
    /// <param name="roomName">Название комнтаы.</param>
    public void RefreshRoom(string roomName)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      ServerRefreshRoomCommand.MessageContent sendingContent = new ServerRefreshRoomCommand.MessageContent() { RoomName = roomName };
      Client.SendMessage(ServerRefreshRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Изменяет администратора комнаты.
    /// </summary>
    /// <param name="roomName">Название комнаты.</param>
    /// <param name="newAdmin">Пользователь назначаемый администратором.</param>
    public void SetRoomAdmin(string roomName, User newAdmin)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (newAdmin == null)
        throw new ArgumentNullException("newAdmin");

      ServerSetRoomAdminCommand.MessageContent sendingContent = new ServerSetRoomAdminCommand.MessageContent() { RoomName = roomName, NewAdmin = newAdmin };
      Client.SendMessage(ServerSetRoomAdminCommand.Id, sendingContent);
    }

    /// <summary>
    /// Добовляет файл на раздачу.
    /// </summary>
    /// <param name="roomName">Название комнаты в которую добавляется файл.</param>
    /// <param name="path">Путь к добовляемому файлу.</param>
    public void AddFileToRoom(string roomName, string path)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(path))
        throw new ArgumentException("fileName");

      FileInfo info = new FileInfo(path);

      if (!info.Exists)
        return;

      PostedFile postedFile;
      lock (Client.PostedFiles)
        postedFile = Client.PostedFiles.FirstOrDefault((posted) => posted.File.Owner.Equals(Client.Info) &&
                                                                   string.Equals(posted.ReadStream.Name, path) &&
                                                                   string.Equals(posted.RoomName, roomName));

      if (postedFile != null)
      {
        ServerAddFileToRoomCommand.MessageContent oldSendingContent = new ServerAddFileToRoomCommand.MessageContent() { RoomName = roomName, File = postedFile.File };
        Client.SendMessage(ServerAddFileToRoomCommand.Id, oldSendingContent);
        return;
      }

      lock (Client.PostedFiles)
      {
        int id = 0;
        while (Client.PostedFiles.Exists((postFile) => postFile.File.ID == id))
          id = idCreator.Next(int.MinValue, int.MaxValue);

        FileDescription file = new FileDescription(Client.Info, info.Length, Path.GetFileName(path), id);

        Client.PostedFiles.Add(new PostedFile()
        {
          File = file,
          RoomName = roomName,
          ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read)
        });

        ServerAddFileToRoomCommand.MessageContent newSendingContent = new ServerAddFileToRoomCommand.MessageContent() { RoomName = roomName, File = file };
        Client.SendMessage(ServerAddFileToRoomCommand.Id, newSendingContent);
      }
    }

    /// <summary>
    /// Удаляет файл с раздачи.
    /// </summary>
    /// <param name="roomName">Название комнаты из которой удаляется файл.</param>
    /// <param name="file">Описание удаляемого файла.</param>
    public void RemoveFileFromRoom(string roomName, FileDescription file)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (file == null)
        throw new ArgumentNullException("file");

      lock (Client.PostedFiles)
      {
        PostedFile postedFile = Client.PostedFiles.FirstOrDefault((current) => current.File.Equals(file));

        if (postedFile == null)
          return;

        Client.PostedFiles.Remove(postedFile);
        postedFile.Dispose();
      }

      ServerRemoveFileFormRoomCommand.MessageContent sendingContent = new ServerRemoveFileFormRoomCommand.MessageContent() { RoomName = roomName, File = file };
      Client.SendMessage(ServerRemoveFileFormRoomCommand.Id, sendingContent);
    }

    /// <summary>
    /// Загружает файл.
    /// </summary>
    /// <param name="path">Путь для сохранения файла.</param>
    /// <param name="roomName">Название комнаты где находится файл.</param>
    /// <param name="file">Описание файла.</param>
    public void DownloadFile(string path, string roomName, FileDescription file)
    {
      if (string.IsNullOrEmpty(roomName))
        throw new ArgumentException("roomName");

      if (string.IsNullOrEmpty(path))
        throw new ArgumentException("path");

      if (file == null)
        throw new ArgumentNullException("file");

      if (File.Exists(path))
        throw new ArgumentException("Файл уже существует");

      if (Client.DownloadingFiles.Exists((dFile) => dFile.File.Equals(file)))
        throw new FileAlreadyDownloadingException(file);

      if (Client.Info.Equals(file.Owner))
        throw new ArgumentException("Нельзя скачивать свой файл.");

      lock (Client.DownloadingFiles)
        Client.DownloadingFiles.Add(new DownloadingFile() { File = file, FullName = path });

      ClientReadFilePartCommand.MessageContent sendingContent = new ClientReadFilePartCommand.MessageContent();
      sendingContent.File = file;
      sendingContent.Length = AsyncClient.DefaultFilePartSize;
      sendingContent.RoomName = roomName;
      sendingContent.StartPartPosition = 0;

      Client.SendMessage(ClientReadFilePartCommand.Id, sendingContent, file.Owner);
    }

    /// <summary>
    /// Останавлиает загрузку файла.
    /// </summary>
    /// <param name="file">Описание файла.</param>
    /// <param name="leaveLoadedPart">Если значение истино недогруженный файл не будет удалятся.</param>
    public void CancelDownloading(FileDescription file, bool leaveLoadedPart)
    {
      if (file == null)
        throw new ArgumentNullException("file");

      DownloadingFile downloadingFile;
      lock (Client.DownloadingFiles)
        downloadingFile = Client.DownloadingFiles.FirstOrDefault((current) => current.File.Equals(file));

      if (downloadingFile == null)
        return;

      string filePath = downloadingFile.FullName;
      downloadingFile.Dispose();

      lock (Client.DownloadingFiles)
        Client.DownloadingFiles.Remove(downloadingFile);

      if (File.Exists(filePath) && !leaveLoadedPart)
        File.Delete(filePath);
    }

    /// <summary>
    /// Иницирует соединение к другому клиенту.
    /// </summary>
    /// <param name="info">Информация о клиенте к которму будет инцированно соединение.</param>
    public void ConnectToPeer(User info)
    {
      ServerP2PConnectRequestCommand.MessageContent sendingContent = new ServerP2PConnectRequestCommand.MessageContent();
      sendingContent.Info = info;
      Client.SendMessage(ServerP2PConnectRequestCommand.Id, sendingContent);
    }
  }
}
