using Engine.Model.Client;
using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Plugins.Client
{
  public class ClientAPIWrapper :
    CrossDomainObject,
    IClientAPI
  {
    public IClientCommand GetCommand(byte[] message) { return ClientModel.API.GetCommand(message); }
    public void SendMessage(string message, string roomName) { ClientModel.API.SendMessage(message, roomName); }
    public void SendPrivateMessage(string receiver, string message) { ClientModel.API.SendPrivateMessage(receiver, message); }
    public void CreateRoom(string roomName, RoomType type) { ClientModel.API.CreateRoom(roomName, type); }
    public void DeleteRoom(string roomName) { ClientModel.API.DeleteRoom(roomName); }
    public void InviteUsers(string roomName, IEnumerable<User> users) { ClientModel.API.InviteUsers(roomName, users); }
    public void KickUsers(string roomName, IEnumerable<User> users) { ClientModel.API.KickUsers(roomName, users); }
    public void ExitFromRoom(string roomName) { ClientModel.API.ExitFromRoom(roomName); }
    public void RefreshRoom(string roomName) { ClientModel.API.RefreshRoom(roomName); }
    public void SetRoomAdmin(string roomName, User newAdmin) { ClientModel.API.SetRoomAdmin(roomName, newAdmin); }
    public void AddFileToRoom(string roomName, string path) { ClientModel.API.AddFileToRoom(roomName, path); }
    public void RemoveFileFromRoom(string roomName, FileDescription file) { ClientModel.API.RemoveFileFromRoom(roomName, file); }
    public void DownloadFile(string path, string roomName, FileDescription file) { ClientModel.API.DownloadFile(path, roomName, file); }
    public void CancelDownloading(FileDescription file, bool leaveLoadedPart) { ClientModel.API.CancelDownloading(file, leaveLoadedPart); }
    public void Register() { ClientModel.API.Register(); }
    public void Unregister() { ClientModel.API.Unregister(); }
    public void PingRequest() { ClientModel.API.PingRequest(); }
    public void ConnectToPeer(string peerId) { ClientModel.API.ConnectToPeer(peerId); }

    public bool IsInited { get { return ClientModel.API != null; } }
  }
}
