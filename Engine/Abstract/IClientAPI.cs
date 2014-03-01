using Engine.Concrete;
using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Engine.Abstract
{
  public interface IClientAPI
  {
    IClientAPICommand GetCommand(byte[] message);
    AsyncClient Client { get; }

    void SendMessage(string message, string roomName);
    void SendPrivateMessage(string receiver, string message);

    void CreateRoom(string roomName);
    void DeleteRoom(string roomName);
    void InviteUsers(string roomName, IEnumerable<User> users);
    void KickUsers(string roomName, IEnumerable<User> users);
    void ExitFormRoom(string roomName);
    void RefreshRoom(string roomName);
    void SetRoomAdmin(string roomName, User newAdmin);

    void AddFileToRoom(string roomName, string path);
    void RemoveFileFromRoom(string roomName, FileDescription file);
    void DownloadFile(string path, string roomName, FileDescription file);
    void CancelDownloading(FileDescription file, bool leaveLoadedPart);

    void SendRegisterRequest(User info, RSAParameters openKey);
    void SendUnregisterRequest();

    void ConnectToPeer(User info);
  }
}
