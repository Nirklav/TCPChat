using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Net;
using System.Collections;
using System.Collections.Generic;

namespace TCPChat.Engine.Connections
{
    public abstract class Connection : IDisposable
    {
        private const int bufferSize = 1024 * 2;

        #region protected fields
        protected int maxReceivedDataSize;
        protected byte[] buffer;
        protected Socket handler;
        protected MemoryStream receivedData;
        protected UserDescription info;
        #endregion

        #region constructors
        protected void Construct(Socket Handler, int MaxReceivedDataSize)        
        {
            if (Handler == null)
                throw new ArgumentNullException();

            if (!Handler.Connected)
                throw new ArgumentException("Сокет должен быть соединен.");

            if (MaxReceivedDataSize < 0)
                throw new ArgumentException("MaxReceivedDataSize должно быть больше 0.");

            handler = Handler;
            maxReceivedDataSize = MaxReceivedDataSize;
            buffer = new byte[bufferSize];
            receivedData = new MemoryStream();

            handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
        }
        #endregion

        #region properties
        public UserDescription Info
        {
            get { return info; }
            set { info = value; }
        }

        public IPEndPoint RemotePoint
        {
            get { return (IPEndPoint)handler.RemoteEndPoint; }
        }
        #endregion

        #region public methods
        public void SendAsync(ushort Id, object MessageContent)
        {
            if (!handler.Connected)
                return;

            try
            {
                MemoryStream MessageStream = new MemoryStream();

                MessageStream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
                MessageStream.Write(BitConverter.GetBytes(Id), 0, sizeof(ushort));

                if (MessageContent != null)
                {
                    BinaryFormatter Formatter = new BinaryFormatter();
                    Formatter.Serialize(MessageStream, MessageContent);
                }

                byte[] MessageToSend = MessageStream.ToArray();
                int MessageToSendSize = (int)MessageStream.Length;
                Buffer.BlockCopy(BitConverter.GetBytes(MessageToSendSize), 0, MessageToSend, 0, sizeof(int));
                handler.BeginSend(MessageToSend, 0, MessageToSend.Length, SocketFlags.None, SendCallback, null);
            }
            catch (SocketException se)
            {
                if (!HandleSocketException(se))
                    throw se;
            }
        }
        #endregion

        #region private callback methods
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int BytesRead = handler.EndReceive(result);

                if (BytesRead > 0)
                {
                    OnPackageReceive();

                    receivedData.Write(buffer, 0, BytesRead);

                    if (DataIsReceived())
                    {
                        OnDataReceived(new DataReceivedEventArgs() { ReceivedData = GetData(), Error = null });
                    }
                    else
                    {
                        if (GetSizeReceivingData() > maxReceivedDataSize)
                        {
                            throw new ReceivedDataException();
                        }
                    }
                }

                handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException se)
            {
                if (!HandleSocketException(se))
                    OnDataReceived(new DataReceivedEventArgs() { ReceivedData = null, Error = se });
            }
            catch (Exception e)
            {
                OnDataReceived(new DataReceivedEventArgs() { ReceivedData = null, Error = e });
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            try
            {
                int SendedDataSize = handler.EndSend(result);
                OnDataSended(new DataSendedEventArgs() { SendedDataCount = SendedDataSize, Error = null });
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException se)
            {
                if (!HandleSocketException(se))
                    OnDataSended(new DataSendedEventArgs() { Error = se });
            }
            catch (Exception e)
            {
                OnDataSended(new DataSendedEventArgs() { Error = e });
            }
        }
        #endregion

        #region protected virtual/abstract methods
        /// <summary>
        /// Происходит когда получено полное сообщение.
        /// </summary>
        /// <param name="args">Инормаци о данных, и данные.</param>
        protected abstract void OnDataReceived(DataReceivedEventArgs args);

        /// <summary>
        /// Происходит при отправке данных. Или при возниконовении ошибки произошедшей во время передачи данных.
        /// </summary>
        /// <param name="args">Информация о отправленных данных.</param>
        protected abstract void OnDataSended(DataSendedEventArgs args);

        /// <summary>
        /// Происходит при получении пакета данных.
        /// </summary>
        protected virtual void OnPackageReceive() {}

        /// <summary>
        /// Происходит при ловле классом SocketException. Без переопределение возращает всегда false.
        /// </summary>
        /// <param name="se">Словленое исключение.</param>
        /// <returns>Вовзращает значение говорящее о том, нужно ли дальше выкидывать исключение, или оно обработано. true - обработано. false - не обработано.</returns>
        protected virtual bool HandleSocketException(SocketException se)
        {
            return false;
        }
        #endregion

        #region private methods
        private byte[] GetData()
        {
            if (!DataIsReceived())
                return null;

            byte[] memoryStreamBuffer = receivedData.GetBuffer();

            byte[] resultData = new byte[GetSizeReceivingData() - sizeof(int)];
            Buffer.BlockCopy(memoryStreamBuffer, sizeof(int), resultData, 0, resultData.Length);

            int restDataSize = (int)(receivedData.Length - GetSizeReceivingData());
            int sizeOfReceivingData = GetSizeReceivingData();

            receivedData.Dispose();
            receivedData = new MemoryStream();

            if (restDataSize > 0)
                receivedData.Write(memoryStreamBuffer, sizeOfReceivingData, restDataSize);

            if (DataIsReceived())
                OnDataReceived(new DataReceivedEventArgs() { ReceivedData = GetData(), Error = null });

            return resultData;
        }

        private bool DataIsReceived()
        {
            int receivingDataSize = GetSizeReceivingData();

            if (receivingDataSize == -1)
                return false;

            if (receivingDataSize > receivedData.Length)
                return false;

            return true;
        }

        private int GetSizeReceivingData()
        {
            if (receivedData.Length < sizeof(int))
                return -1;

            int DataSize = BitConverter.ToInt32(receivedData.GetBuffer(), 0);

            return DataSize;
        }

        protected void SendAsync(byte[] data)
        {
            try
            {
                MemoryStream MessageStream = new MemoryStream();
                MessageStream.Write(BitConverter.GetBytes(data.Length + sizeof(int)), 0, sizeof(int));
                MessageStream.Write(data, 0, data.Length);

                byte[] Message = MessageStream.ToArray();
                MessageStream.Dispose();

                handler.BeginSend(Message, 0, Message.Length, SocketFlags.None, SendCallback, null);
            }
            catch (SocketException se)
            {
                if (!HandleSocketException(se))
                    throw se;
            }
        }
        #endregion

        #region IDisposable
        bool disposed = false;

        protected virtual void ReleaseResource()
        {
            if (disposed) return;

            disposed = true;

            if (handler != null)
            {
                handler.Close();
            }

            if (receivedData != null)
                receivedData.Dispose();
        }

        public void Dispose()
        {
            ReleaseResource();
        }
        #endregion
    }
}
