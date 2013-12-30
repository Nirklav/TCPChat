using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Linq;
using TCPChat.Dialogs;
using TCPChat.Engine;
using TCPChat.Engine.Connections;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace TCPChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region consts
        const string ProgramName = "TCPChat";
        const string ServerAlreadyRunning = "Сервер уже запущен. Остановите сервер для выполнения операции.";
        const string ServerNotRunnig = "Сервер и так не запущен.";
        const string ParamsError = "Ошибка входных данных.";
        const string ClientAlreadyConnected = "Клиент уже соединен с сервером. Разорвите соединение для выполнения операции.";
        const string ClientDicsonnected = "Клиент и так не соедниен с сервером.";
        const string ClientNotCreated = "Клинет не соединен ни с каким сервером. Установите соединение.";
        const string RegFailNickAlreadyExist = "Ник уже занят, вы не зарегистрированны.";
        const string RegSuccessful = "Регистрация успешна.";
        const string RoomExitQuestion = "Вы действительно хотите выйти из комнаты?";
        const string RoomCloseQuestion = "Вы точно хотите закрыть комнату?";
        const string ServerDisableQuestion = "Вы точно хотите выключить сервер?";
        const string CancelDownloadingQuestion = "Вы уже загружаете этот файл. Вы хотите отменить загрузку?";
        const string FileNotFound = "Файл недоступен";
        const string InviteInRoom = "Пригласить в комнату";
        const string KickFormRoom = "Удалить из комнаты";
        const string NoBodyToInvite = "Некого пригласить. Все и так в комнате.";
        const string FileMustDontExist = "Необходимо выбрать несуществующий файл.";
        const string AllInRoom = "Все в комнате";

        const int ClientMaxMessageLength = 100 * 1024;
        const string ServerLogFile = "ServerErrors.log";
        const string FileDialogFilter = "Все файлы|*.*";
        #endregion

        #region fields
        AsyncServer server;
        ClientConnection client;
        TextBox messageField;
        ComboBox receiverField;
        ScrollViewer messagesScroll;
        SynchronizationContext GUIContext;

        string lastNick;
        Color lastNickColor;
        string lastAddress;
        int lastPort;
        bool lastStateOfIPv6Protocol;
        #endregion

        #region constructor
        public MainWindow()
        {
            InitializeComponent();

            ChatRooms.Items.Add(new RoomContainer(new RoomDescription(null, AsyncServer.MainRoomName), MessageList_Changed));
            GUIContext = SynchronizationContext.Current;

            LoadSettings();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        #endregion

        #region loaded/closed events
        private void MessagesListScroll_Loaded(object sender, RoutedEventArgs e)
        {
            messagesScroll = (ScrollViewer)sender;
        }

        private void MessageField_Loaded(object sender, RoutedEventArgs e)
        {
            messageField = (TextBox)sender;
        }

        private void ReceiverField_Loaded(object sender, RoutedEventArgs e)
        {
            receiverField = (ComboBox)sender;
            RefreshReceiversList();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveSettings();

            if (client != null)
            {
                try
                {
                    client.SendUnregisterRequest();
                }
                catch (SocketException) { }

                client.Dispose();
            }

            if (server != null)
                server.Dispose();
        }
        #endregion

        #region UI clicks
        private void EnableServer_Click(object sender, RoutedEventArgs e)
        {
            if (server != null)
            {
                GetCurrentRoom().AddSystemMessage(ServerAlreadyRunning);
                return;
            }

            if (client != null)
            {
                GetCurrentRoom().AddSystemMessage(ClientAlreadyConnected);
                return;
            }

            ServerDialog dialog = new ServerDialog(lastNick, lastNickColor, lastPort, lastStateOfIPv6Protocol);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    lastNick = dialog.Nick;
                    lastNickColor = dialog.NickColor;
                    lastPort = dialog.Port;
                    lastStateOfIPv6Protocol = dialog.UsingIPv6Protocol;

                    server = new AsyncServer(ServerLogFile);
                    server.Start(dialog.Port, dialog.UsingIPv6Protocol);

                    CreateClient(dialog.Nick);
                    client.Info.NickColor = dialog.NickColor;
                    client.Connect(new IPEndPoint((dialog.UsingIPv6Protocol) ? IPAddress.IPv6Loopback : IPAddress.Loopback, dialog.Port));
                }
                catch (ArgumentException)
                {
                    GetCurrentRoom().AddSystemMessage(ParamsError);

                    if (server != null)
                    {
                        server.Dispose();
                        server = null;
                    }

                    if (client != null)
                    {
                        client.Dispose();
                        client = null;
                    }
                }
            }
        }

        private void DisableServer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ServerDisableQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            if (server == null)
            {
                GetCurrentRoom().AddSystemMessage(ServerNotRunnig);
                return;
            }

            if (client != null)
            {
                client.Dispose();
                client = null;
            }

            server.Dispose();
            server = null;

            ChatRooms.Items.Clear();
            ChatRooms.Items.Add(new RoomContainer(new RoomDescription(null, AsyncServer.MainRoomName), MessageList_Changed));
            ChatRooms.SelectedIndex = 0;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                GetCurrentRoom().AddSystemMessage(ClientAlreadyConnected);
                return;
            }

            ConnectDialog dialog = new ConnectDialog(lastNick, lastNickColor, lastAddress, lastPort);
            if (dialog.ShowDialog() == true)
            {
                lastNick = dialog.Nick;
                lastNickColor = dialog.NickColor;
                lastPort = dialog.Port;
                lastAddress = dialog.Address.ToString();

                CreateClient(dialog.Nick);
                client.Info.NickColor = dialog.NickColor;
                client.Connect(new IPEndPoint(dialog.Address, dialog.Port));
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (client == null)
            {
                GetCurrentRoom().AddSystemMessage(ClientDicsonnected);
                return;
            }

            try
            {
                client.SendUnregisterRequest();
            }
            catch(SocketException) { }

            client.Dispose();
            client = null;

            ChatRooms.Items.Clear();
            ChatRooms.Items.Add(new RoomContainer(new RoomDescription(null, AsyncServer.MainRoomName), MessageList_Changed));
            ChatRooms.SelectedIndex = 0;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateRoomDialog dialog = new CreateRoomDialog();
                if (dialog.ShowDialog() == true)
                    client.CreateRoom(dialog.RoomName);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void DeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show(this, RoomCloseQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                client.DeleteRoom(GetCurrentRoom().RoomName);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void InvateInRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RoomContainer mainRoom = GetRoom(AsyncServer.MainRoomName);

                IEnumerable<UserContainer> availableUsers = mainRoom.UsersCollection.Except(GetCurrentRoom().UsersCollection);
                if (availableUsers.Count() == 0)
                {
                    GetCurrentRoom().AddSystemMessage(NoBodyToInvite);
                    return;
                }

                UsersOperationDialog dialog = new UsersOperationDialog(InviteInRoom, availableUsers);
                if (dialog.ShowDialog() == true)
                    client.InviteUsers(GetCurrentRoom().RoomName, dialog.Users);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void KickFormRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UsersOperationDialog dialog = new UsersOperationDialog(KickFormRoom, GetCurrentRoom().UsersCollection);
                if (dialog.ShowDialog() == true)
                    client.KickUsers(GetCurrentRoom().RoomName, dialog.Users);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void ExitFormRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show(this, RoomExitQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                client.ExitFormRoom(GetCurrentRoom().RoomName);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void SetRoomAdmin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newAdminNick = (string)((MenuItem)sender).Tag;
                UserDescription newAdmin = GetUser(newAdminNick).Info;
                client.SetRoomAdmin(((RoomContainer)ChatRooms.SelectedContent).RoomName, newAdmin);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void MessageField_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                if (messageField.Text == string.Empty) return;

                if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                {
                    try
                    {
                        if (receiverField.SelectedIndex == 0)
                            client.SendMessage(messageField.Text, GetCurrentRoom().RoomName);
                        else
                        {
                            string receiverNick = receiverField.SelectedValue.ToString();

                            client.SendPrivateMessage(receiverNick, messageField.Text);

                            UserContainer senderUser = GetUser(client.Info.Nick);
                            UserContainer receiverUser = GetUser(receiverNick);

                            GetCurrentRoom().AddMessage(senderUser, receiverUser, messageField.Text, true);
                        }

                        messageField.Text = string.Empty;
                    }
                    catch (NullReferenceException)
                    {
                        GetCurrentRoom().AddSystemMessage(ClientNotCreated);
                    }
                    catch (SocketException se)
                    {
                        GetCurrentRoom().AddSystemMessage(se.Message);
                    }
                }
                else
                {
                    messageField.Text += Environment.NewLine;
                    messageField.CaretIndex = messageField.Text.Length;
                }
            }
        }

        private void DownloadFile_Click(object sender, MouseButtonEventArgs e)
        {
            FileDescription file = (FileDescription)((TextBlock)sender).Tag;

            try
            {
                if (file == null)
                {
                    GetCurrentRoom().AddSystemMessage(FileNotFound);
                    return;
                }

                if (client.DownloadingFiles.Exists((dFile) => dFile.File.Equals(file)))
                    throw new FileAlreadyDownloadingException(file);

                if (client.Info.Equals(file.Owner))
                    throw new ArgumentException("Нельзя скачивать свой файл.");

                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.OverwritePrompt = false;
                saveDialog.Filter = FileDialogFilter;
                saveDialog.FileName = file.Name;

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    client.DownloadFile(saveDialog.FileName, ((RoomContainer)ChatRooms.SelectedContent).RoomName, file);
            }
            catch (FileAlreadyDownloadingException)
            {
                if (MessageBox.Show(CancelDownloadingQuestion, ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    client.CancelDownloading(file, true);

                    MessageContainer message = GetCurrentRoom().MessagesCollection.FirstOrDefault((current) => file.Equals(current.File));

                    if (message == null)
                        return;

                    message.Progress = 0;
                }
            }
            catch (ArgumentException ae)
            {
                GetCurrentRoom().AddSystemMessage(ae.Message);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.Filter = FileDialogFilter;

                if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    client.AddFileToRoom(GetCurrentRoom().RoomName, openDialog.FileName);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void File_PreviewDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Handled = true;
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void File_PreviewDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (fileNames == null)
                    return;

                for (int i = 0; i < fileNames.Count(); i++)
                    if (e.Data.GetDataPresent(DataFormats.FileDrop) && fileNames[i].Contains("."))
                        client.AddFileToRoom(GetCurrentRoom().RoomName, fileNames[i]);
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void Files_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (client == null)
                    throw new NullReferenceException();

                PostedFilesDialog dialog = new PostedFilesDialog(client);
                dialog.ShowDialog();
            }
            catch (NullReferenceException)
            {
                GetCurrentRoom().AddSystemMessage(ClientNotCreated);
            }
            catch (SocketException se)
            {
                GetCurrentRoom().AddSystemMessage(se.Message);
            }
        }

        private void AboutProgram_Click(object sender, RoutedEventArgs e)
        {
            AboutProgramDialog dialog = new AboutProgramDialog();
            dialog.ShowDialog();
        }

        private void User_Click(object sender, MouseButtonEventArgs e)
        {
            TextBlock userTextBlock = sender as TextBlock;

            if (userTextBlock == null)
                return;

            messageField.Text += userTextBlock.Text + ", ";
            messageField.CaretIndex = messageField.Text.Length;
        }

        private void ChatRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            RoomContainer selectedRoom = e.AddedItems[0] as RoomContainer;

            if (selectedRoom == null)
                return;

            selectedRoom.Updated = false;

            if (messagesScroll != null)
                messagesScroll.ScrollToEnd();
        }

        private void MessageList_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (messagesScroll.VerticalOffset == messagesScroll.ScrollableHeight)
            {
                messagesScroll.ScrollToEnd();
            }
        }
        #endregion

        #region client events
        private void Client_Registration(object sender, RegistrationEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                RegistrationEventArgs args = (RegistrationEventArgs)Obj;         
   
                if (!args.Registered)
                {
                    GetCurrentRoom().AddSystemMessage(RegFailNickAlreadyExist);

                    client.Dispose();
                    client = null;
                }
            }, e);
        }

        private void Client_RoomOpened(object sender, RoomEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                RoomEventArgs args = (RoomEventArgs)Obj;

                if (GetRoom(args.Room.Name) != null)
                    return;

                RoomContainer room = new RoomContainer(args.Room, client.Info.Nick, MessageList_Changed);
                ChatRooms.Items.Add(room);
                room.Updated = true;

                Alert();
            }, e);
        }

        private void Client_RoomClosed(object sender, RoomEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                RoomEventArgs args = (RoomEventArgs)Obj;

                RoomContainer deletingRoom = GetRoom(args.Room.Name);

                if (deletingRoom == null)
                    return;

                ChatRooms.Items.Remove(deletingRoom);
                Alert();
            }, e);
        }

        private void Client_RoomRefreshed(object sender, RoomEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                RoomEventArgs args = (RoomEventArgs)Obj;
                RoomContainer refreshedRoom = GetRoom(args.Room.Name);

                if (refreshedRoom == null)
                    return;

                refreshedRoom.Refresh(args.Room, client.Info.Nick);

                if (args.Room.Name == AsyncServer.MainRoomName)
                    RefreshReceiversList();
            }, e);
        }

        private void Client_ReceiveMessage(object sender, ReceiveMessageEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                ReceiveMessageEventArgs args = (ReceiveMessageEventArgs)Obj;

                RoomContainer room = null;

                if (args.IsPrivateMessage || args.IsSystemMessage)
                    room = GetCurrentRoom();
                else
                    room = GetRoom(args.RoomName);

                if (room != null && !args.IsSystemMessage && !args.IsFileMessage)
                {
                    UserContainer senderUser = GetUser(args.Sender);
                    UserContainer receiverUser = GetUser(client.Info.Nick);
                    room.AddMessage(senderUser, receiverUser, args.Message, args.IsPrivateMessage);
                }

                if (room != null && !args.IsSystemMessage && args.IsFileMessage)
                {
                    UserContainer senderUser = GetUser(args.Sender);
                    room.AddFileMessage(senderUser, e.Message, (FileDescription)e.State);
                }

                if (room != null && args.IsSystemMessage && !args.IsFileMessage)
                {
                    room.AddSystemMessage(args.Message);
                }

                if (!string.Equals(room.RoomName, GetCurrentRoom().RoomName))
                    room.Updated = true;

                Alert();
            }, e);
        }

        private void Client_DownloadProgress(object sender, FileDownloadEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                FileDownloadEventArgs args = (FileDownloadEventArgs)Obj;
                RoomContainer room = GetRoom(args.RoomName);

                if (room == null)
                    return;

                MessageContainer message = room.MessagesCollection.FirstOrDefault((current) => args.File.Equals(current.File));

                if (message == null)
                    return;

                if (args.Progress >= 100)
                {
                    message.Progress = 0;
                    room.AddSystemMessage(string.Format("Загрузка файла \"{0}\" завершена.", args.File.Name));
                }
                else
                {
                    message.Progress = args.Progress;
                }
            }, e);
        }

        private void Client_PostedFileDeleted(object sender, FileDownloadEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                FileDownloadEventArgs args = (FileDownloadEventArgs)Obj;
                RoomContainer room = GetRoom(args.RoomName);

                if (room == null)
                    return;

                IEnumerable<MessageContainer> messages = room.MessagesCollection.Where((current) => args.File.Equals(current.File));

                if (messages.Count() == 0)
                    return;

                foreach (MessageContainer message in messages)
                {
                    message.Progress = 0;
                    message.File = null;
                }
            }, e);
        }

        private void Client_Connect(object sender, ConnectEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                ConnectEventArgs args = (ConnectEventArgs)Obj;

                if (args.Error != null)
                {
                    GetCurrentRoom().AddSystemMessage(args.Error.Message);
                    client.Dispose();
                    client = null;
                    return;
                }

                client.SendRegisterRequest();
            }, e);
        }

        private void Client_AsyncError(object sender, AsyncErrorEventArgs e)
        {
            GUIContext.Post(Obj =>
            {
                AsyncErrorEventArgs args = (AsyncErrorEventArgs)Obj;

                if (args.Error.GetType() == typeof(APINotSupprtedException))
                {
                    client.Dispose();
                    client = null;
                }

                GetCurrentRoom().AddSystemMessage(args.Error.Message);
            }, e);
        }
        #endregion

        #region utilit methods
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        private void Alert()
        {
            if ((IsActive && !(WindowState == WindowState.Minimized)) || !AlertsMenuBtn.IsChecked)
                return;

            WindowInteropHelper h = new WindowInteropHelper(this);
            FlashWindow(h.Handle, true);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception error = e.ExceptionObject as Exception;

            if (error == null)
                return;

            Logger logger = new Logger("UnhandledError.log");
            logger.Write(error);
        }

        private RoomContainer GetCurrentRoom()
        {
            return (RoomContainer)ChatRooms.SelectedContent;
        }

        private RoomContainer GetRoom(string name)
        {
            return ChatRooms.Items.Cast<RoomContainer>().FirstOrDefault((current) => string.Equals(current.RoomName, name));
        }

        private UserContainer GetUser(string name)
        {
            return GetRoom(AsyncServer.MainRoomName).UsersCollection.FirstOrDefault((user) => string.Equals(user.Nick, name));
        }

        private void RefreshReceiversList()
        {
            receiverField.Items.Clear();
            receiverField.Items.Add(AllInRoom);

            foreach (UserDescription user in GetRoom(AsyncServer.MainRoomName).Info.Users)
                if (!user.Equals(client.Info))
                    receiverField.Items.Add(user.Nick);

            receiverField.SelectedIndex = 0;
        }

        private void CreateClient(string nick)
        {
            client = new ClientConnection(nick);
            client.Connected += Client_Connect;
            client.ReceiveMessage += Client_ReceiveMessage;
            client.ReceiveRegistrationResponse += Client_Registration;
            client.RoomRefreshed += Client_RoomRefreshed;
            client.AsyncError += Client_AsyncError;
            client.RoomClosed += Client_RoomClosed;
            client.RoomOpened += Client_RoomOpened;
            client.DownloadProgress += Client_DownloadProgress;
            client.PostedFileDeleted += Client_PostedFileDeleted;
        }

        private void LoadSettings()
        {
            try
            {
                XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml");
                lastNick = doc.Element("Settings").Element("Nick").Value;

                byte r, g, b;
                r = byte.Parse(doc.Element("Settings").Element("NickColor").Element("R").Value);
                g = byte.Parse(doc.Element("Settings").Element("NickColor").Element("G").Value);
                b = byte.Parse(doc.Element("Settings").Element("NickColor").Element("B").Value);
                lastNickColor = Color.FromArgb(r, g, b);

                lastAddress = doc.Element("Settings").Element("Address").Value;
                lastPort = int.Parse(doc.Element("Settings").Element("Port").Value);
                lastStateOfIPv6Protocol = bool.Parse(doc.Element("Settings").Element("StateOfIPv6Protocol").Value);
                AlertsMenuBtn.IsChecked = bool.Parse(doc.Element("Settings").Element("Alert").Value);
                Width = int.Parse(doc.Element("Settings").Element("FormSize").Element("Width").Value);
                Height = int.Parse(doc.Element("Settings").Element("FormSize").Element("Height").Value);
                
            }
            catch
            {
                lastNick = string.Empty;
                lastNickColor = Color.FromArgb(170, 50, 50);
                lastAddress = string.Empty;
                lastPort = 10060;
                lastStateOfIPv6Protocol = false;
            }
        }

        private void SaveSettings()
        {
            XDocument doc = new XDocument(new XElement("Settings",
                new XElement("Nick", lastNick),
                new XElement("NickColor",
                    new XElement("R", lastNickColor.R),
                    new XElement("G", lastNickColor.G),
                    new XElement("B", lastNickColor.B)),
                new XElement("Address", lastAddress),
                new XElement("Port", lastPort),
                new XElement("StateOfIPv6Protocol", lastStateOfIPv6Protocol),
                new XElement("Alert", AlertsMenuBtn.IsChecked),
                new XElement("FormSize",
                    new XElement("Width", Width),
                    new XElement("Height", Height))));

            doc.Save(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml");
        }
        #endregion
    }
}
