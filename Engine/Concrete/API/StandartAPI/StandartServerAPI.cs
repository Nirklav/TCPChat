using Engine.Abstract;
using Engine.Concrete.Connections;
using Engine.Concrete.Containers;
using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Concrete.API.StandartAPI
{
  /// <summary>
  /// Класс реазиующий стандартное серверное API.
  /// </summary>
  public class StandartServerAPI : IServerAPI
  {
    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public const string API = "StandartAPI v1.3";

    /// <summary>
    /// Сервер являющийся хозяином данного API.
    /// </summary>
    public AsyncServer Server { get; private set; }

    /// <summary>
    /// Список поданых запросов ожидающих ответа от клиентов.
    /// </summary>
    public Dictionary<int, ServerConnection> FilePartRequests { get; private set; }

    private Dictionary<ushort, IServerAPICommand> commandDictionary = new Dictionary<ushort, IServerAPICommand>();

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    /// <param name="host">Сервер которому будет принадлежать данный API.</param>
    public StandartServerAPI(AsyncServer host)
    {
      Server = host;
      FilePartRequests = new Dictionary<int, ServerConnection>();

      commandDictionary.Add(ServerRegisterCommand.Id, new ServerRegisterCommand());
      commandDictionary.Add(ServerUnregisterCommand.Id, new ServerUnregisterCommand());
      commandDictionary.Add(ServerSendRoomMessageCommand.Id, new ServerSendRoomMessageCommand());
      commandDictionary.Add(ServerSendOneUserCommand.Id, new ServerSendOneUserCommand());
      commandDictionary.Add(ServerSendUserOpenKeyCommand.Id, new ServerSendUserOpenKeyCommand());
      commandDictionary.Add(ServerCreateRoomCommand.Id, new ServerCreateRoomCommand());
      commandDictionary.Add(ServerDeleteRoomCommand.Id, new ServerDeleteRoomCommand());
      commandDictionary.Add(ServerInviteUsersCommand.Id, new ServerInviteUsersCommand());
      commandDictionary.Add(ServerKickUsersCommand.Id, new ServerKickUsersCommand());
      commandDictionary.Add(ServerExitFormRoomCommand.Id, new ServerExitFormRoomCommand());
      commandDictionary.Add(ServerRefreshRoomCommand.Id, new ServerRefreshRoomCommand());
      commandDictionary.Add(ServerSetRoomAdminCommand.Id, new ServerSetRoomAdminCommand());
      commandDictionary.Add(ServerAddFileToRoomCommand.Id, new ServerAddFileToRoomCommand());
      commandDictionary.Add(ServerRemoveFileFormRoomCommand.Id, new ServerRemoveFileFormRoomCommand());
      commandDictionary.Add(ServerP2PConnectRequestCommand.Id, new ServerP2PConnectRequestCommand());
      commandDictionary.Add(ServerP2PConnectResponceCommand.Id, new ServerP2PConnectResponceCommand());
      commandDictionary.Add(ServerPingRequest.Id, new ServerPingRequest());
    }

    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public string APIName
    {
      get { return API; }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Пришедшее сообщение, по которому будет определена необходимая для извлекания команда.</param>
    /// <returns>Команда для выполнения.</returns>
    public IServerAPICommand GetCommand(byte[] message)
    {
      ushort id = BitConverter.ToUInt16(message, 0);

      try
      {
        return commandDictionary[id];
      }
      catch (KeyNotFoundException)
      {
        return ServerEmptyCommand.Empty;
      }
    }

    /// <summary>
    /// Напрямую соединяет пользователей.
    /// </summary>
    /// <param name="container"></param>
    public void IntroduceConnections(ConnectionsContainer container)
    {
      ClientWaitPeerConnectionCommand.MessageContent content = new ClientWaitPeerConnectionCommand.MessageContent();
      content.RequestPoint = container.RequestPeerPoint;
      content.SenderPoint = container.SenderPeerPoint;
      content.RemoteInfo = container.SenderConnection.Info;
      content.ServiceConnectId = container.Id;

      container.RequestConnection.SendMessage(ClientWaitPeerConnectionCommand.Id, content);
    }

    /// <summary>
    /// Посылает системное сообщение клиенту.
    /// </summary>
    /// <param name="receiveConnection">Соединение которое получит сообщение.</param>
    /// <param name="message">Сообщение.</param>
    public void SendSystemMessage(ServerConnection receiveConnection, string message)
    {
      ClientOutSystemMessageCommand.MessageContent sendingContent = new ClientOutSystemMessageCommand.MessageContent() { Message = message };
      receiveConnection.SendMessage(ClientOutSystemMessageCommand.Id, sendingContent);
    }

    /// <summary>
    /// Закрывает соединение.
    /// </summary>
    /// <param name="nick">Ник пользователя, соединение котрого будет закрыто.</param>
    public void CloseConnection(string nick)
    {
      ServerConnection closingConnection = Server.Connections.Find((connection) => string.Equals(nick, connection.Info.Nick));

      if (closingConnection == null)
        return;

      CloseConnection(closingConnection);
    }

    /// <summary>
    /// Закрывает соединение.
    /// </summary>
    /// <param name="connection">Соединение которое будет закрыто.</param>
    public void CloseConnection(ServerConnection connection)
    {
      lock (Server.Connections)
      {
        Server.Connections.Remove(connection);

        lock (Server.Rooms)
        {
          foreach (string roomName in Server.Rooms.Keys)
          {
            Room room = Server.Rooms[roomName];

            if (!room.Users.Contains(connection.Info))
              continue;

            room.Users.Remove(connection.Info);

            foreach (User user in room.Users)
            {
              if (user == null)
                continue;

              ServerConnection userConnection = Server.Connections.Find((conn) => user.Equals(conn.Info));
              ClientRoomRefreshedCommand.MessageContent sendingContent = new ClientRoomRefreshedCommand.MessageContent() { Room = room };
              userConnection.SendMessage(ClientRoomRefreshedCommand.Id, sendingContent);
            }
          }
        }
      }

      connection.Dispose();
    }
  }
}
