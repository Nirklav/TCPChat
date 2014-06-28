using Engine.API.StandardAPI.ClientCommands;
using Engine.API.StandardAPI.ServerCommands;
using Engine.Containers;
using Engine.Model.Entities;
using Engine.Model.Server;
using Engine.Network;
using Engine.Network.Connections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace Engine.API.StandardAPI
{
  /// <summary>
  /// Класс реазиующий стандартное серверное API.
  /// </summary>
  public class StandardServerAPI : IServerAPI
  {
    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public const string API = "StandartAPI v2.0";

    private Dictionary<ushort, IServerAPICommand> commandDictionary = new Dictionary<ushort, IServerAPICommand>();

    /// <summary>
    /// Список поданых запросов ожидающих ответа от клиентов.
    /// </summary>
    internal Dictionary<int, ServerConnection> FilePartRequests { get; private set; }

    /// <summary>
    /// Создает экземпляр API.
    /// </summary>
    /// <param name="host">Сервер которому будет принадлежать данный API.</param>
    public StandardServerAPI()
    {
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
      commandDictionary.Add(ServerPingRequestCommand.Id, new ServerPingRequestCommand());
    }

    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public string Name
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

      IServerAPICommand command;
      if (commandDictionary.TryGetValue(id, out command))
        return command;

      return ServerEmptyCommand.Empty;
    }

    /// <summary>
    /// Напрямую соединяет пользователей.
    /// </summary>
    /// <param name="container"></param>
    public void IntroduceConnections(string senderId, IPEndPoint senderPoint, string requestId, IPEndPoint requestPoint)
    {
      using (var context = ServerModel.Get())
      {
        var content = new ClientWaitPeerConnectionCommand.MessageContent
        {
          RequestPoint = requestPoint,
          SenderPoint = senderPoint,
          RemoteInfo = context.Users[senderId],
        };

        ServerModel.Server.SendMessage(requestId, ClientWaitPeerConnectionCommand.Id, content);
      }
    }

    /// <summary>
    /// Посылает системное сообщение клиенту.
    /// </summary>
    /// <param name="nick">Пользователь получащий сообщение.</param>
    /// <param name="message">Сообщение.</param>
    public void SendSystemMessage(string nick, string message)
    {
      var sendingContent = new ClientOutSystemMessageCommand.MessageContent { Message = message };
      ServerModel.Server.SendMessage(nick, ClientOutSystemMessageCommand.Id, sendingContent);
    }

    /// <summary>
    /// Закрывает соединение.
    /// </summary>
    /// <param name="nick">Ник пользователя, соединение котрого будет закрыто.</param>
    public void CloseConnection(string nick)
    {
      ServerModel.Server.CloseConnection(nick);

      using (var server = ServerModel.Get())
      {
        foreach (string roomName in server.Rooms.Keys)
        {
          Room room = server.Rooms[roomName];

          if (!room.Users.Contains(nick))
            continue;

          room.Remove(nick);
          server.Users.Remove(nick);

          foreach (string user in room.Users)
          {
            if (user == null)
              continue;

            var sendingContent = new ClientRoomRefreshedCommand.MessageContent
            {
              Room = room,
              Users = room.Users.Select(n => server.Users[n]).ToList()
            };
            ServerModel.Server.SendMessage(user, ClientRoomRefreshedCommand.Id, sendingContent);
          }
        }
      }
    }
  }
}
