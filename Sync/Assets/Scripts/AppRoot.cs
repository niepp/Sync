using UnityEngine;
//using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections;
using System.Collections.Generic;

public class AppRoot : MonoBehaviour
{
    const string playerAPath = "PlayerA";
    const string playerBPath = "PlayerB";

#if !DEDICATED_SERVER
    public string quickHost = "127.0.0.1";
    public int quickPort = 7100;
    NetworkConnection m_conn;

    private static bool m_isConnected = false;
    public static bool IsConnected
    {
        get
        {
            return m_isConnected;
        }
    }

    void OnGUI()
    {
        if (!m_isConnected)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 125, Screen.height / 2 - 60, 250, 120), "ConnectServer"))
            {
                StartClient();
                m_isConnected = true;
            }
        }
    }

    void StartClient()
    {
        var prefabObjA = Resources.Load<GameObject>(playerAPath);
        ClientScene.RegisterPrefab(prefabObjA);

        var prefabObjB = Resources.Load<GameObject>(playerBPath);
        ClientScene.RegisterPrefab(prefabObjB);

        NetworkClient client = new NetworkClient();
        client.RegisterHandler(MsgType.Connect, OnConnectedServer);
        client.RegisterHandler(MsgType.Disconnect, OnDisConnectedServer);
        client.RegisterHandler(MsgType.Error, OnError);
        client.Connect(quickHost, quickPort);
    }

    void OnConnectedServer(NetworkMessage netMsg)
    {
        Debug.LogWarning("connecte to server, connId:" + netMsg.conn.connectionId);
        m_conn = netMsg.conn;
        ClientScene.Ready(netMsg.conn);
    }

    void OnDisConnectedServer(NetworkMessage netMsg)
    {
    }

    void OnError(NetworkMessage netMsg)
    {
    }

#else

    const int connCountLimit = 2;
    const int listenPort = 7100;
    int playerCount = 0;
    List<NetworkConnection> m_conns = new List<NetworkConnection>();

    void Start()
    {
        LaunchServer();
    }

    void LaunchServer()
    {
        NetworkServer.RegisterHandler(MsgType.Connect, OnClientConnected);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnClientDisConnected);
        NetworkServer.RegisterHandler(MsgType.Error, OnError);

        bool succeed = NetworkServer.Listen(listenPort);
        if (succeed)
            Debug.LogWarning("Server Start Success!");
        else
            Debug.LogErrorFormat("Server Start Failed! port:{0}", listenPort);
    }

    void OnClientConnected(NetworkMessage netMsg)
    {
        Debug.LogWarning("Player " + playerCount + " connected from address:" + netMsg.conn.address + "\n connId:" + netMsg.conn.connectionId);
        m_conns.Add(netMsg.conn);
        AddPlayer(netMsg);
    }

    void OnClientDisConnected(NetworkMessage netMsg)
    {
    }

    void OnError(NetworkMessage netMsg)
    {
    }

    void AddPlayer(NetworkMessage netMsg)
    {
        var resPath = (playerCount % 2 == 0) ? playerAPath : playerBPath;
        var prefabObj = Resources.Load<GameObject>(resPath);
        var bornPos = (playerCount % 2 == 0) ? new Vector3(-2.0f, 0, 0) : new Vector3(2.0f, 0, 0);

        GameObject thePlayer = (GameObject)Instantiate(prefabObj, bornPos, Quaternion.identity);
        // This spawns the new player on all clients
        NetworkServer.AddPlayerForConnection(netMsg.conn, thePlayer, 0);
        ++playerCount;
    }

#endif
}
