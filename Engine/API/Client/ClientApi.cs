using Engine.Model.Client;
using Engine.Model.Common.Entities;
using Engine.Plugins;
using Engine.Plugins.Client;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;

namespace Engine.Api.Client
{
  public sealed class ClientApi :
    CrossDomainObject,
    IApi
  {
    [SecurityCritical] private readonly Dictionary<long, ICommand> _commands;
    [SecurityCritical] private long _lastSendedNumber;

    /// <summary>
    /// Creates new instance of ClientApi.
    /// </summary>
    [SecurityCritical]
    public ClientApi()
    {
      ClientModel.Recorder.Recorded += OnRecorded;

      _commands = new Dictionary<long, ICommand>();
      AddCommand(new ClientRegistrationResponseCommand());
      AddCommand(new ClientRoomRefreshedCommand());
      AddCommand(new ClientOutPrivateMessageCommand());
      AddCommand(new ClientOutRoomMessageCommand());
      AddCommand(new ClientOutSystemMessageCommand());
      AddCommand(new ClientFilePostedCommand());
      AddCommand(new ClientRoomOpenedCommand());
      AddCommand(new ClientRoomClosedCommand());
      AddCommand(new ClientFileRemovedCommand());
      AddCommand(new ClientReadFilePartCommand());
      AddCommand(new ClientWriteFilePartCommand());
      AddCommand(new ClientPingResponseCommand());
      AddCommand(new ClientConnectToPeerCommand());
      AddCommand(new ClientWaitPeerConnectionCommand());
      AddCommand(new ClientConnectToP2PServiceCommand());
      AddCommand(new ClientPlayVoiceCommand());
    }

    [SecurityCritical]
    private void AddCommand(ICommand command)
    {
      _commands.Add(command.Id, command);
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      ClientModel.Recorder.Recorded -= OnRecorded;
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

      using (var client = ClientModel.Get())
      {
        var userNick = client.Chat.User.Nick;

        foreach (var user in client.Chat.GetUsers())
        {
          if (!user.IsVoiceActive())
            continue;

          if (user.Nick == userNick)
            continue;

          ClientModel.Peer.SendMessageIfConnected(user.Nick, ClientPlayVoiceCommand.CommandId, content, true);
        }
      }
    }

    /// <summary>
    /// Name and version of Api.
    /// </summary>
    public string Name
    {
      [SecuritySafeCritical]
      get { return Api.Name; }
    }

    /// <summary>
    /// Get the command by identifier.
    /// </summary>
    /// <param name="id">Command identifier.</param>
    /// <returns>Command.</returns>
    [SecuritySafeCritical]
    public ICommand GetCommand(long id)
    {
      ICommand command;
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
