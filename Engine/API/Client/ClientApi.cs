using Engine.Api.Server;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Plugins;
using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;

namespace Engine.Api.Client
{
  public sealed class ClientApi :
    CrossDomainObject,
    IApi<ClientCommandArgs>
  {
    [SecurityCritical] private readonly Dictionary<long, ICommand<ClientCommandArgs>> _commands;
    [SecurityCritical] private readonly Dictionary<string, int> _interlocutors;
    [SecurityCritical] private long _lastSendedNumber;

    /// <summary>
    /// Creates new instance of ClientApi.
    /// </summary>
    [SecurityCritical]
    public ClientApi()
    {
      _commands = new Dictionary<long, ICommand<ClientCommandArgs>>();
      _interlocutors = new Dictionary<string, int>();

      ClientModel.Recorder.Recorded += OnRecorded;

      AddCommand(new ClientRegistrationResponseCommand());
      AddCommand(new ClientRoomRefreshedCommand());
      AddCommand(new ClientOutPrivateMessageCommand());
      AddCommand(new ClientOutRoomMessageCommand());
      AddCommand(new ClientOutSystemMessageCommand());
      AddCommand(new ClientFilePostedCommand());
      AddCommand(new ClientRoomOpenedCommand());
      AddCommand(new ClientRoomClosedCommand());
      AddCommand(new ClientPostedFileDeletedCommand());
      AddCommand(new ClientReadFilePartCommand());
      AddCommand(new ClientWriteFilePartCommand());
      AddCommand(new ClientPingResponceCommand());
      AddCommand(new ClientConnectToPeerCommand());
      AddCommand(new ClientWaitPeerConnectionCommand());
      AddCommand(new ClientConnectToP2PServiceCommand());
      AddCommand(new ClientPlayVoiceCommand());
    }

    [SecurityCritical]
    private void AddCommand(ICommand<ClientCommandArgs> command)
    {
      _commands.Add(command.Id, command);
    }

    [SecurityCritical]
    private void OnRecorded(object sender, RecordedEventArgs e)
    {
      if (!ClientModel.IsInited)
        return;

      var data = new byte[e.DataSize];
      Buffer.BlockCopy(e.Data, 0, data, 0, data.Length);

      var content = new ClientPlayVoiceCommand.MessageContent
      {
        Pack = new SoundPack
        {
          Data = data,
          Channels = e.Channels,
          BitPerChannel = e.BitPerChannel,
          Frequency = e.Frequency
        },
        Number = Interlocked.Increment(ref _lastSendedNumber)
      };

      string userNick;
      using (var client = ClientModel.Get())
        userNick = client.Chat.User.Nick;

      lock (_interlocutors)
      {
        foreach (var kvp in _interlocutors)
        {
          var nick = kvp.Key;
          var count = kvp.Value;

          if (count <= 0)
            continue;

          if (nick.Equals(userNick))
            continue;

          ClientModel.Peer.SendMessageIfConnected(nick, ClientPlayVoiceCommand.CommandId, content, true);
        }
      }
    }

    /// <summary>
    /// Возвращает флаг описывающий активность собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    /// <returns>Флаг описывающий активность собеседника.</returns>
    [SecuritySafeCritical]
    public bool IsActiveInterlocutor(string nick)
    {
      lock (_interlocutors)
      {
        int count;
        _interlocutors.TryGetValue(nick, out count);
        return count > 0;
      }
    }

    /// <summary>
    /// Добавляет собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    [SecuritySafeCritical]
    public void AddInterlocutor(string nick)
    {
      lock (_interlocutors)
      {
        int count;
        _interlocutors.TryGetValue(nick, out count);
        _interlocutors[nick] = count + 1;
      }
    }

    /// <summary>
    /// Удаляет собеседника.
    /// </summary>
    /// <param name="nick">Ник собеседника.</param>
    [SecuritySafeCritical]
    public void RemoveInterlocutor(string nick)
    {
      lock (_interlocutors)
      {
        int count;
        _interlocutors.TryGetValue(nick, out count);
        if (count == 0)
          throw new InvalidOperationException("Can't remove interlocutor");

        if (count == 1)
        {
          _interlocutors.Remove(nick);
          return;
        }

        _interlocutors[nick] = count - 1;
      }
    }

    /// <summary>
    /// Версия и имя данного API.
    /// </summary>
    public string Name
    {
      [SecuritySafeCritical]
      get { return Api.Name; }
    }

    /// <summary>
    /// Извлекает команду.
    /// </summary>
    /// <param name="message">Сообщение, по которому будет определена команда.</param>
    /// <returns>Команда.</returns>
    [SecuritySafeCritical]
    public ICommand<ClientCommandArgs> GetCommand(long id)
    {
      ICommand<ClientCommandArgs> command;
      if (_commands.TryGetValue(id, out command))
        return command;

      ClientPluginCommand pluginCommand;
      if (ClientModel.Plugins.TryGetCommand(id, out pluginCommand))
        return pluginCommand;

      return ClientEmptyCommand.Empty;
    }

    /// <summary>
    /// Perform the remote action.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    public void Perform(IAction action)
    {
      action.Perform();
    }
  }
}
