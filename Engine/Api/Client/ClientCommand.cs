using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Client.Entities;
using Engine.Model.Common.Dto;
using Engine.Network;
using Engine.Plugins;
using System.Collections.Generic;
using System.Security;

namespace Engine.Api.Client
{
  public abstract class ClientCommand : 
    CrossDomainObject, 
    ICommand
  {
    public abstract long Id
    {
      [SecuritySafeCritical]
      get;
    }

    protected virtual bool IsPeerCommand
    {
      [SecuritySafeCritical]
      get { return false; }
    }

    [SecuritySafeCritical]
    public void Run(CommandArgs args)
    {
      if (IsPeerCommand)
      {
        if (args.ConnectionId == AsyncClient.ClientId)
          throw new ModelException(ErrorCode.IllegalInvoker, string.Format("Command cannot be runned from server package. {0}", GetType().FullName));
      }
      else
      {
        if (args.ConnectionId != AsyncClient.ClientId)
          throw new ModelException(ErrorCode.IllegalInvoker, string.Format("Command cannot be runned from peer package. {0}", GetType().FullName));
      }

      OnRun(args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(CommandArgs args);
  }

  public abstract class ClientCommand<TContent> : ClientCommand
  {
    [SecuritySafeCritical]
    protected sealed override void OnRun(CommandArgs args)
    {
      var package = args.Unpacked.Package as IPackage<TContent>;
      if (package == null)
        throw new ModelException(ErrorCode.WrongContentType);

      OnRun(package.Content, args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(TContent content, CommandArgs args);

    #region Helpers
    /// <summary>
    /// Adds not existing users to chat.
    /// </summary>
    /// <param name="chat">Chat.</param>
    /// <param name="users">User dtos.</param>
    [SecuritySafeCritical]
    protected void AddUsers(ClientChat chat, IEnumerable<UserDto> users)
    {
      foreach (var userDto in users)
      {
        if (!chat.IsUserExist(userDto.Nick))
        {
          var clientUser = new ClientUser(userDto);
          chat.AddUser(clientUser);
          ClientModel.Peer.RegisterPeer(clientUser.Nick, clientUser.Certificate);
        }
      }
    }

    /// <summary>
    /// Removes not existing users from chat.
    /// </summary>
    /// <param name="chat">Chat.</param>
    /// <param name="users">Users that be removed.</param>
    [SecuritySafeCritical]
    protected void RemoveUsers(ClientChat chat, IEnumerable<string> users)
    {
      foreach (var nick in users)
      {
        chat.RemoveUser(nick);
        ClientModel.Peer.UnregisterPeer(nick);
      }
    }
    #endregion
  }
}
