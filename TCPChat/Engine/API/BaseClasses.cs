using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TCPChat.Engine.Connections;

namespace TCPChat.Engine.API
{
    interface IServerAPICommand
    {
        void Run(ServerCommandArgs args);
    }

    interface IClientAPICommand
    {
        void Run(ClientCommandArgs args);
    }

    class ServerCommandArgs
    {
        public ServerConnection UserConnection { get; set; }
        public IServerAPI API { get; set; }
        public byte[] Message { get; set; }
    }

    class ClientCommandArgs
    {
        public PeerConnection Peer { get; set; }
        public IClientAPI API { get; set; }
        public byte[] Message { get; set; }
    }

    interface IServerAPI
    {
        string APIName { get; }

        void AddCommand(ushort id, IServerAPICommand command);
        IServerAPICommand GetCommand(byte[] message);
        AsyncServer Server { get; }

        void SendSystemMessage(ServerConnection receiveConnection, string message);
        void CloseConnection(string nick);
        void CloseConnection(ServerConnection connection);
    }

    interface IClientAPI
    {
        void AddCommand(ushort id, IClientAPICommand command);
        IClientAPICommand GetCommand(byte[] message);
        ClientConnection Client { get; }

        void SendMessage(string message, string roomName);
        void SendPrivateMessage(string receiver, string message);

        void CreateRoom(string roomName);
        void DeleteRoom(string roomName);
        void InviteUsers(string roomName, IEnumerable<UserDescription> users);
        void KickUsers(string roomName, IEnumerable<UserDescription> users);
        void ExitFormRoom(string roomName);
        void RefreshRoom(string roomName);
        void SetRoomAdmin(string roomName, UserDescription newAdmin);

        void AddFileToRoom(string roomName, string path);
        void RemoveFileFromRoom(string roomName, FileDescription file);
        void DownloadFile(string path, string roomName, FileDescription file);
        void CancelDownloading(FileDescription file, bool leaveLoadedPart);

        void SendRegisterRequest(UserDescription info, RSAParameters openKey);
        void SendUnregisterRequest();

        void ConnectToPeer(UserDescription info);
    }
}
