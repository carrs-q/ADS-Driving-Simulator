using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable]
public class SimClient
{
    public static int MAX_ID;

    public int ID;
    public string name;
}

public class ConnectedClient
{
    public SimClient simClient;
    public TcpClient tcpClient;

    public ConnectedClient(SimClient simClient, TcpClient tcpClient)
    {
        this.simClient = simClient;
        this.tcpClient = tcpClient;
    }

}

[Serializable]
public class ServerMessage
{
    public SimClient simClient;
    public string message;

    public ServerMessage(SimClient simClient, string message)
    {
        this.simClient = simClient;
        this.message = message;
    }
}


public class Server :  MonoBehaviour
{
    public Action<string> OnLog = delegate { };
    public Action<ConnectedClient> OnClientDisconnect = delegate { };
    
    public bool IsConnected()
    {
        //get { return tcpListenerThread != null && tcpListenerThread.IsAlive};
         return tcpListener != null && tcpListener.Server.IsBound;
    }

    private string IPAddress = "127.0.0.1";
    private int Port = 8052;
    private TcpListener tcpListener;

    private List<ConnectedClient> connectedClients = new List<ConnectedClient>();

    public void StartServer(string IPAddress, int Port)
    {
        this.IPAddress = IPAddress;
        this.Port = Port;
        try
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, Port);
            tcpListener.Start();

            ThreadPool.QueueUserWorkItem(ListenerWorker, tcpListener);

            Debug.Log("Server started at: " + this.IPAddress + ":" + this.Port);
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException);
        }
    }
    private void ListenerWorker(object token)
    {
        while (tcpListener != null)
        {
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            

            ThreadPool.QueueUserWorkItem(HandleClientWorker, tcpClient);
        }
        Debug.Log("tcpListener went out");
    }
    private void HandleClientWorker(object token)
    {
        Byte[] bytes = new Byte[Controller.BUFFERSIZE];
        using (TcpClient client = token as TcpClient)
        {
            SimClient simclient = new SimClient();
            simclient.ID = ++SimClient.MAX_ID;
            simclient.name = "TMP" + simclient.ID;



            ConnectedClient connectedClient = new ConnectedClient(simclient, client);
            connectedClients.Add(connectedClient);

            //Just send a random message
            SendMessageToClient(simclient.ID, "Welcome Stranger, who are you?");

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    int length;
                    while (stream.CanRead && (length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incomingData = new byte[length];
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        string clientMessage = Encoding.ASCII.GetString(incomingData);

                        /*if (clientMessage == "!disconnect")
                        { 
                            stream.Close();
                            client.Close();
                        }*/

                        ProcessMessage(connectedClient, clientMessage);
                    }
                    Debug.Log("ich bin ausgelaufen");
                }
            }
            catch (SocketException e)
            {
                Debug.Log(e.ToString());
            }
        }
        Debug.Log("Ich bin ausgelaufen 2");
    }
    private void ProcessMessage(ConnectedClient connectedClient, string command)
    {
        string[] split = command.Split('|');
        string response = string.Empty;

        switch (split[0])
        {
            case Controller.RESDISPLAY: //Resond Display
                connectedClient.simClient.name = split[1]; //Assign correct name
                OnLog(split[0]+"|"+connectedClient.simClient.ID +"|"+ split[1]);
                break;
            case "!disconnect":
                response = (string.Format("{0} has Disconnected", connectedClient.simClient.name));
                Debug.Log(response);
                DisconnectClient(connectedClient);
                break;
            default:
                OnLog(command);
                break;
        }

    }

    private void CheckForData(TcpClient c)
    {
        Debug.Log("still connected");
        Byte[] bytes = new Byte[Controller.BUFFERSIZE];
        //begin receiving data from the client
        c.Client.BeginReceive(bytes, 0, Controller.BUFFERSIZE, 0, ReadCallback, c);

    }
    void ReadCallback(IAsyncResult ar)
    {

        Debug.Log("test");

    }

    public void serverUpdate()
    {
        connectedClients.ForEach(delegate (ConnectedClient c)
        {

            if (c.tcpClient != null && c.tcpClient.Connected)
            {
                CheckForData(c.tcpClient);
            }
            else
            {
                c.tcpClient.Close(); //close the socket
                connectedClients.Remove(c);
                Debug.Log("Client disconnected");
                //disconnectList.Add(sc);
                //continue;
            }
        });
    }

    private void DispatchMessage(ServerMessage serverMessage)
    {
        for (int i = 0; i < connectedClients.Count; i++)
        {
            ConnectedClient connection = connectedClients[i];
            TcpClient client = connection.tcpClient;
            if (!SendMessage(client, serverMessage))
            {
                Debug.Log(string.Format("Lost connection with {0}", connection.simClient.name));
                DisconnectClient(connection);
                i--;
            }
        }
    }
    private void DisconnectClient(ConnectedClient connection)
    {
        OnClientDisconnect(connection);
        connectedClients.Remove(connection);
    }
    public void SendMessageToClient(int clientID, string message)
    {
        ServerMessage tmp;
        Debug.Log("Start sending Message to " + clientID);

        connectedClients.ForEach(delegate (ConnectedClient c)
        {
            if (c.simClient.ID == clientID)
            {
                tmp = new ServerMessage(c.simClient, message);
                if(!SendMessage(c.tcpClient, tmp)){
                    Debug.Log("Error sending Message" + message);
                }
            }
        });
    }
    public void SendMessageToAllClients(string message)
    {
        ServerMessage tmp;
        try
        {
            connectedClients.ForEach(delegate (ConnectedClient c)
            {
                Debug.Log(message + " to " + c.simClient.name);
                tmp = new ServerMessage(c.simClient, message);
                TcpClient tempClient = c.tcpClient;
                if (!SendMessage(tempClient, tmp))
                {
                    Debug.Log(string.Format("Lost connection with {0}", c.simClient.name));
                    DisconnectClient(c);
                }
            });
        }
        catch(Exception e)
        {
           // TODO: This exception will be thrown always when a client got removed, not nice
           //Debug.Log("Client got disconnected: " +e);
        }
    }
    private bool SendMessage(TcpClient client, ServerMessage serverMessage)
    {
        if (client != null && client.Connected)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                if (stream.CanWrite)
                {
                    //Create Json Byte Array from Serializable Object 
                    byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(JsonUtility.ToJson(serverMessage));
                    stream.WriteAsync(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                    Debug.Log("Message sent:" + serverMessage.message);
                    return true;
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }
        else
        {
            // The client is null or not connected anymore
            // In this case the connection got dropped
            Debug.Log("It kicks me out here");
        }
        return false;
    }
    private void StopServer()
    {
        tcpListener.Stop();

        connectedClients.ForEach(delegate(ConnectedClient c){
            DisconnectClient(c);
        });
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }
    private void OnDestroy()
    {
        StopServer();
    }
}