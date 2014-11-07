using Engine.Audio;
using Engine.Model.Entities;
using Engine.Network;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Engine
{
  public interface IClientAPI
  {
    IClientCommand GetCommand(byte[] message);

    void SendMessage(string message, string roomName);
    void SendPrivateMessage(string receiver, string message);

    void CreateRoom(string roomName, RoomType type);
    void DeleteRoom(string roomName);
    void InviteUsers(string roomName, IEnumerable<User> users);
    void KickUsers(string roomName, IEnumerable<User> users);
    void ExitFromRoom(string roomName);
    void RefreshRoom(string roomName);
    void SetRoomAdmin(string roomName, User newAdmin);

    void AddFileToRoom(string roomName, string path);
    void RemoveFileFromRoom(string roomName, FileDescription file);
    void DownloadFile(string path, string roomName, FileDescription file);
    void CancelDownloading(FileDescription file, bool leaveLoadedPart);

    void Register();
    void Unregister();

    void PingRequest();

    void ConnectToPeer(string peerId);
  }
}
