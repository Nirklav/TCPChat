using Engine.API;
using Engine.Model.Client;
using Engine.Model.Entities;
using System.Collections.Generic;
using System.Security;

namespace Engine.Plugins.Client
{
  [SecuritySafeCritical]
  public class ClientAPIWrapper :
    CrossDomainObject,
    IClientAPI
  {
    public bool IsInited
    {
      [SecuritySafeCritical]
      get { return ClientModel.API != null; }
    }

    [SecuritySafeCritical]
    public ICommand<ClientCommandArgs> GetCommand(byte[] message)
    {
      return ClientModel.API.GetCommand(message);
    }

    [SecuritySafeCritical]
    public void SendMessage(long? messageId, string message, string roomName)
    {
      ClientModel.API.SendMessage(messageId, message, roomName);
    }

    [SecuritySafeCritical]
    public void SendPrivateMessage(string receiver, string message)
    {
      ClientModel.API.SendPrivateMessage(receiver, message);
    }

    [SecuritySafeCritical]
    public void CreateRoom(string roomName, RoomType type)
    {
      ClientModel.API.CreateRoom(roomName, type);
    }

    [SecuritySafeCritical]
    public void DeleteRoom(string roomName)
    {
      ClientModel.API.DeleteRoom(roomName);
    }

    [SecuritySafeCritical]
    public void InviteUsers(string roomName, IEnumerable<User> users)
    {
      ClientModel.API.InviteUsers(roomName, users);
    }

    [SecuritySafeCritical]
    public void KickUsers(string roomName, IEnumerable<User> users)
    {
      ClientModel.API.KickUsers(roomName, users);
    }

    [SecuritySafeCritical]
    public void ExitFromRoom(string roomName)
    {
      ClientModel.API.ExitFromRoom(roomName);
    }

    [SecuritySafeCritical]
    public void RefreshRoom(string roomName)
    {
      ClientModel.API.RefreshRoom(roomName);
    }

    [SecuritySafeCritical]
    public void SetRoomAdmin(string roomName, User newAdmin)
    {
      ClientModel.API.SetRoomAdmin(roomName, newAdmin);
    }

    [SecuritySafeCritical]
    public void AddFileToRoom(string roomName, string path)
    {
      ClientModel.API.AddFileToRoom(roomName, path);
    }

    [SecuritySafeCritical]
    public void RemoveFileFromRoom(string roomName, FileDescription file)
    {
      ClientModel.API.RemoveFileFromRoom(roomName, file);
    }

    [SecuritySafeCritical]
    public void DownloadFile(string path, string roomName, FileDescription file)
    {
      ClientModel.API.DownloadFile(path, roomName, file);
    }

    [SecuritySafeCritical]
    public void CancelDownloading(FileDescription file, bool leaveLoadedPart)
    {
      ClientModel.API.CancelDownloading(file, leaveLoadedPart);
    }

    [SecuritySafeCritical]
    public void Register()
    {
      ClientModel.API.Register();
    }

    [SecuritySafeCritical]
    public void Unregister()
    {
      ClientModel.API.Unregister();
    }

    [SecuritySafeCritical]
    public void PingRequest()
    {
      ClientModel.API.PingRequest();
    }

    [SecuritySafeCritical]
    public void ConnectToPeer(string peerId)
    {
      ClientModel.API.ConnectToPeer(peerId);
    }
  }
}
