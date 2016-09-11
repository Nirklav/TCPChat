using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Client.Entities;
using Engine.Model.Common.Dto;
using Engine.Model.Common.Entities;
using Engine.Network;
using Engine.Plugins;
using System.Collections.Generic;
using System.Security;

namespace Engine.Api.Client
{
  public abstract class ClientCommand : 
    CrossDomainObject, 
    ICommand<ClientCommandArgs>
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
    public void Run(ClientCommandArgs args)
    {
      if (IsPeerCommand)
      {
        if (args.PeerConnectionId == null)
          throw new ModelException(ErrorCode.IllegalInvoker, string.Format("Command cannot be runned from server package. {0}", GetType().FullName));
      }
      else
      {
        if (args.PeerConnectionId != null)
          throw new ModelException(ErrorCode.IllegalInvoker, string.Format("Command cannot be runned from peer package. {0}", GetType().FullName));
      }

      OnRun(args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(ClientCommandArgs args);
  }

  public abstract class ClientCommand<TContent> : ClientCommand
  {
    [SecuritySafeCritical]
    protected sealed override void OnRun(ClientCommandArgs args)
    {
      var package = args.Unpacked.Package as IPackage<TContent>;
      if (package == null)
        throw new ModelException(ErrorCode.WrongContentType);

      OnRun(package.Content, args);
    }

    [SecuritySafeCritical]
    protected abstract void OnRun(TContent content, ClientCommandArgs args);

    #region Helpers
    /// <summary>
    /// Add not existing users to chat.
    /// </summary>
    /// <param name="chat">Chat.</param>
    /// <param name="users">User dtos.</param>
    [SecuritySafeCritical]
    protected void AddUsers(ClientChat chat, List<UserDto> users)
    {
      foreach (var userDto in users)
      {
        if (!chat.IsUserExist(userDto.Nick))
          chat.AddUser(new User(userDto));
      }
    }
    #endregion
  }
}
