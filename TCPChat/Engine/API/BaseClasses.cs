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

        void SendMessageAsync(string message, string roomName);
        void SendPrivateMessageAsync(string receiver, string message);

        void CreateRoomAsync(string roomName);
        void DeleteRoomAsync(string roomName);
        void InviteUsersAsync(string roomName, IEnumerable<UserDescription> users);
        void KickUsersAsync(string roomName, IEnumerable<UserDescription> users);
        void ExitFormRoomAsync(string roomName);
        void RefreshRoomAsync(string roomName);
        void SetRoomAdmin(string roomName, UserDescription newAdmin);

        void AddFileToRoomAsyc(string roomName, string fileName);
        void RemoveFileFromRoomAsyc(string roomName, FileDescription file);
        void DownloadFile(string path, string roomName, FileDescription file);
        void CancelDownloading(FileDescription file, bool leaveLoadedPart);

        void SendRegisterRequestAsync(UserDescription info, RSAParameters openKey);
        void SendUnregisterRequestAsync();
    }
}
