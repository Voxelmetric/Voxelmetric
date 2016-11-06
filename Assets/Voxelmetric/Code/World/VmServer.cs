﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using SendBlockChange = VmNetworking.SendBlockChange;
using RequestChunkData = VmNetworking.RequestChunkData;
using TransmitChunkData = VmNetworking.TransmitChunkData;

public class VmServer {

    protected World world;
    private IPAddress serverIP;
    private Socket serverSocket;

    private Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();
    private int nextId = 0;

    private bool debugServer = false;

    public IPAddress ServerIP { get { return serverIP; } }

    public int ClientCount {
        get {
            lock (clients) {
                return clients.Count;
            }
        }
    }

    public VmServer(World world)
    {
        this.world = world;

        try
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            serverSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            string serverName = Dns.GetHostName();
            if (debugServer) Debug.Log("serverName='" + serverName + "'");
            foreach (IPAddress serverAddress in Dns.GetHostAddresses(serverName)) {
                if (debugServer) Debug.Log("serverAddress='" + serverAddress + "', AddressFamily=" + serverAddress.AddressFamily);
                if (serverAddress.AddressFamily !=  addressFamily)
                    continue;
                serverIP = serverAddress;
                break;
            }
            IPEndPoint serverEndPoint = new IPEndPoint(serverIP, 8000);
            serverSocket.Bind(serverEndPoint);
            serverSocket.Listen(0);
            serverSocket.BeginAccept(new AsyncCallback(OnJoinServer), null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private void OnJoinServer(IAsyncResult ar)
    {
        try
        {
            if (serverSocket == null) {
                Debug.Log("VmServer.OnJoinServer (" + Thread.CurrentThread.ManagedThreadId + "): "
                    + "client connection rejected because server was not started");
                return;
            }
            Socket newClientSocket = serverSocket.EndAccept(ar);
            lock(clients) {
                ClientConnection connection = new ClientConnection(clients.Count, newClientSocket, this);
                clients.Add(nextId, connection);
                nextId++;
            }

            serverSocket.BeginAccept(new AsyncCallback(OnJoinServer), null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    internal void RemoveClient(int id) {
        lock(clients) {
            clients[id] = null;
        }
    }

    public void Disconnect()
    {
        lock(clients) {
            var clientConnections = clients.Values.ToList();
            foreach (var clientConnection in clientConnections) {
                if (clientConnection != null)
                    clientConnection.Disconnect();
            }
        }
        if (serverSocket != null) {// && serverSocket.Connected) {
            //serverSocket.Shutdown(SocketShutdown.Both);
            serverSocket.Close();
            serverSocket = null;
        }
    }

    public void SendToClient(byte[] data, int client)
    {
        lock (clients) {
            ClientConnection clientConnection = clients[client];
            if ( clientConnection != null )
                clientConnection.Send(data);
        }
    }

    public void RequestChunk(BlockPos pos, int id)
    {
        Chunk chunk = null;
        if (world == null) {
            Debug.LogError("VmServer.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): "
                + " world not set (" + pos + ", " + id + ")");
        } else
            chunk = world.chunks.Get(pos);
        byte[] data;
        // for now return the empty chunk if the request chunk isn't yet loaded
        // Todo: load the chunk then send it to the player
        if (chunk == null) {
            Debug.LogError("VmServer.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): "
                + "Could not find chunk for " + pos);
            chunk = world.EmptyChunk;
            chunk.StartLoading();
        }
        data = chunk.blocks.ToBytes();
        if ( debugServer )
            Debug.Log("VmServer.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): " + id
                + " " + pos);

        SendChunk(pos, data, id);
    }

    protected void SendChunk(BlockPos pos, byte[] chunkData, int id)
    {
        int chunkDataIndex = 0;
        while (chunkDataIndex < chunkData.Length) {
            int remaining = chunkData.Length - chunkDataIndex;
            int size;
            if ( TransmitChunkData.UseVariableMessageLength )
                size = Math.Min(VmNetworking.bufferLength, TransmitChunkData.HeaderSize + remaining);
            else
                size = VmNetworking.bufferLength;
            byte[] message = new byte[size];
            message[0] = TransmitChunkData.ID;
            BitConverter.GetBytes(size).CopyTo(message, TransmitChunkData.IdxSize);
            pos.ToBytes().CopyTo(message, TransmitChunkData.IdxChunkPos);
            BitConverter.GetBytes(chunkDataIndex).CopyTo(message, TransmitChunkData.IdxDataOffset);
            BitConverter.GetBytes(chunkData.Length).CopyTo(message, TransmitChunkData.IdxDataLength);

            if ( debugServer )
                Debug.Log("VmServer.SendChunk (" + Thread.CurrentThread.ManagedThreadId + "): " + pos
                    + ", chunkDataIndex=" + chunkDataIndex
                    + ", chunkData.Length=" + chunkData.Length
                    + ", buffer=" + message.Length
                    + ", size=" + size);

            int idx = TransmitChunkData.IdxData;
            for (; idx < message.Length; idx++) {
                message[idx] = chunkData[chunkDataIndex];
                chunkDataIndex++;

                if (chunkDataIndex >= chunkData.Length) {
                    break;
                }
            }

            if (debugServer) {
                int messageLength = message.Length;
                if (idx < messageLength)
                    messageLength = idx + 1;
                if (size != messageLength) {
                    Debug.Log("VmServer.SendChunk messageLength =" + messageLength
                        + ", size=" + size);
                }
                //BitConverter.GetBytes(messageLength).CopyTo(message, TransmitChunkData.IdxSize);
            }

            SendToClient(message, id);
        }
    }

    public void BroadcastChange(BlockPos pos, Block block, int excludedUser)
    {
        lock(clients) {
            if (clients.Count == 0) {
                return;
            }

            byte[] data = new byte[SendBlockChange.Size];

            data[0] = SendBlockChange.ID;
            pos.ToBytes().CopyTo(data, SendBlockChange.IdxBlockPos);
            BitConverter.GetBytes(block.Type).CopyTo(data, SendBlockChange.IdxBlockType);

            foreach (var client in clients.Values) {
                if (excludedUser == -1 || client.ID != excludedUser) {
                    client.Send(data);
                }
            }
        }
    }

    public void ReceiveChange(BlockPos pos, ushort type, int id)
    {
        Block block = Block.New(type, world);
        world.blocks.Set(pos, block, updateChunk: true, setBlockModified: true);
        BroadcastChange(pos, block, id);
    }
}

internal class ClientConnection : VmSocketState.IMessageHandler {

    private int id;
    private Socket socket;
    private VmServer server;

    private bool debugClientConnection = false;

    public int ID { get { return id; } }

    public ClientConnection(int id, Socket socket, VmServer server) {
        this.id = id;
        this.socket = socket;
        this.server = server;
        if ( debugClientConnection )
            Debug.Log("ClientConnection.ClientConnection (" + Thread.CurrentThread.ManagedThreadId + "): "
                + "Client " + id + " has connected");

        VmSocketState socketState = new VmSocketState(this);
        socket.BeginReceive(socketState.buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromClient), socketState);
    }

    private void OnReceiveFromClient(IAsyncResult ar) {
        try {
            if (socket == null || !socket.Connected) {
                Debug.Log("ClientConnection.OnReceiveFromClient (" + Thread.CurrentThread.ManagedThreadId + "): "
                    + "client message rejected because connection was shutdown or not started");
                return;
            }
            int received = socket.EndReceive(ar);

            if (received == 0) {
                Disconnect();
                return;
            }

            if (debugClientConnection)
                Debug.Log("ClientConnection.OnReceiveFromClient (" + Thread.CurrentThread.ManagedThreadId + "): " + id);

            VmSocketState socketState = ar.AsyncState as VmSocketState;
            socketState.Receive(received);
            if (socket != null && socket.Connected) {// Should be able to use a mutex but unity doesn't seem to like it
                socket.BeginReceive(socketState.buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromClient), socketState);
            }
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
    }

    public int GetExpectedSize(byte type) {
        switch (type) {
            case SendBlockChange.ID:
                return SendBlockChange.Size;
            case RequestChunkData.ID:
                return RequestChunkData.Size;
            default:
                return 0;
        }
    }

    public void HandleMessage(byte[] receivedData) {
        BlockPos pos;

        switch (receivedData[0]) {
            case SendBlockChange.ID:
                pos = BlockPos.FromBytes(receivedData, SendBlockChange.IdxBlockPos);
                ushort type = BitConverter.ToUInt16(receivedData, SendBlockChange.IdxBlockType);
                server.ReceiveChange(pos, type, id);
                break;
            case RequestChunkData.ID:
                pos = BlockPos.FromBytes(receivedData, RequestChunkData.IdxBlockPos);

                if (debugClientConnection)
                    Debug.Log("ClientConnection.HandleMessage (" + Thread.CurrentThread.ManagedThreadId + "): " + id
                        + " " + pos);

                server.RequestChunk(pos, id);
                break;
        }
    }

    public void Send(byte[] buffer) {
        try {
            socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnSend), socket);
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
    }

    private void OnSend(IAsyncResult ar) {
        try {
            socket.EndSend(ar);
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
    }

    public void Disconnect() {
        if (debugClientConnection)
            Debug.Log("ClientConnection.Disconnect (" + Thread.CurrentThread.ManagedThreadId + "): "
                + "Client " + id + " has disconnected");
        try {
            if (socket != null) {// && socket.Connected) {
                //socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
        server.RemoveClient(id);
    }
}
