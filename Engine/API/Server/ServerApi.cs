using Engine.Api.Server.Admin;
using Engine.Api.Server.Files;
using Engine.Api.Server.Messages;
using Engine.Api.Server.Others;
using Engine.Api.Server.P2P;
using Engine.Api.Server.Registrations;
using Engine.Api.Server.Rooms;
using Engine.Model.Common;
using Engine.Model.Server;
using Engine.Plugins;
using Engine.Plugins.Server;
using System.Collections.Generic;
using System.Security;

namespace Engine.Api.Server
{
  public sealed class ServerApi :
    CrossDomainObject,
    IApi
  {
    [SecurityCritical] private readonly Dictionary<long, ICommand> _commands;
    [SecurityCritical] private IServerEvents _events;

    /// <summary>
    /// Creates instance of ServerApi.
    /// </summary>
    /// <param name="adminPassword">Password for chat admin actions.</param>
    [SecurityCritical]
    public ServerApi(string adminPassword)
    {
      _events = NotifierGenerator.MakeEvents<IServerEvents>();
      _events.ConnectionClosing += OnConnectionClosing;
      ServerModel.Notifier.Add(_events);

      _commands = new Dictionary<long, ICommand>();
      AddCommand(new ServerRegisterCommand());
      AddCommand(new ServerUnregisterCommand());
      AddCommand(new ServerSendRoomMessageCommand());
      AddCommand(new ServerCreateRoomCommand());
      AddCommand(new ServerDeleteRoomCommand());
      AddCommand(new ServerInviteUsersCommand());
      AddCommand(new ServerKickUsersCommand());
      AddCommand(new ServerExitFromRoomCommand());
      AddCommand(new ServerRefreshRoomCommand());
      AddCommand(new ServerSetRoomAdminCommand());
      AddCommand(new ServerAddFileToRoomCommand());
      AddCommand(new ServerRemoveFileFromRoomCommand());
      AddCommand(new ServerP2PConnectRequestCommand());
      AddCommand(new ServerP2PReadyAcceptCommand());
      AddCommand(new ServerPingRequestCommand());
      AddCommand(new ServerAdminCommand(adminPassword));
    }

    [SecurityCritical]
    private void AddCommand(ICommand command)
    {
      _commands.Add(command.Id, command);
    }

    [SecuritySafeCritical]
    public void Dispose()
    {
      ServerModel.Notifier.Remove(_events);
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

      ServerPluginCommand pluginCommand;
      if (ServerModel.Plugins.TryGetCommand(id, out pluginCommand))
        return pluginCommand;

      return ServerEmptyCommand.Empty;
    }

    /// <summary>
    /// Perform the remote action.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    public void Perform(IAction action)
    {
      action.Perform();
    }

    #region server events
    private void OnConnectionClosing(object sender, ConnectionEventArgs e)
    {
      Perform(new ServerRemoveUserAction(e.Id, false));
    }
    #endregion
  }
}
