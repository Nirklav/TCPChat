using Engine.Api.Server;
using Engine.Model.Client;
using Engine.Model.Common.Dto;
using System;
using System.Security;

namespace Engine.Api.Client.Registrations
{
  [Serializable]
  public class ClientRegisterAction : IAction
  {
    /// <summary>
    /// Send registration request.
    /// </summary>
    [SecuritySafeCritical]
    public ClientRegisterAction()
    {
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      using (var client = ClientModel.Get())
      {
        var sendingContent = new ServerRegisterCommand.MessageContent { UserDto = new UserDto(client.Chat.User) };
        ClientModel.Client.SendMessage(ServerRegisterCommand.CommandId, sendingContent);
      }
    }
  }
}
