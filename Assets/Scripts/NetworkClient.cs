using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public string PlayerID;

    public GameObject PlayerPrefab;

    GameObject playerGO;
    NetInfo playerInfo;

    [SerializeField]
    List<GameObject> AllPlayersGO = new List<GameObject>();

    
    void Start ()
    {
        Debug.Log("Initialized.");

        PlayerID = "Player " + System.DateTime.Now.ToString() + UnityEngine.Random.value.ToString();

        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("Connected to the server.");
        SpawnPlayer();
        InvokeRepeating("HandShake", 0.0f, 2.0f);
    }

    void SpawnPlayer()
    {
        Debug.Log("Player spawned.");

        Vector3 pos = new Vector3(UnityEngine.Random.Range(-2.0f, 2.0f), 0.0f, 0.0f);

        playerGO = Instantiate(PlayerPrefab, pos, new Quaternion());
        playerInfo = playerGO.GetComponent<NetInfo>();
        playerInfo.localID = m_Connection.InternalId.ToString();
        AllPlayersGO.Add(playerGO);

        //// Example to send a handshake message:
        PlayerSpawnMsg m = new PlayerSpawnMsg();
        m.Position = pos;
        m.ID = PlayerID;
        SendToServer(JsonUtility.ToJson(m));
    }

    void SpawnOtherPlayer(PlayerSpawnMsg msg)
    {
        if(msg.ID != PlayerID)
        {
            GameObject otherPlayerGO = Instantiate(PlayerPrefab, msg.Position, new Quaternion());
            AllPlayersGO.Add(otherPlayerGO);
        }
    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                //Debug.Log("Handshake message received!");
            break;

            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Player update message received!");
            break;

            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
            break;

            case Commands.REQUEST_ID:
                RequestIDMsg riMsg = JsonUtility.FromJson<RequestIDMsg>(recMsg);
                playerInfo.serverID = riMsg.ID;
                Debug.Log("Request ID message received!");
            break;

            case Commands.PLAYER_SPAWN:
                PlayerSpawnMsg psMsg = JsonUtility.FromJson<PlayerSpawnMsg>(recMsg);
                SpawnOtherPlayer(psMsg);
            break;

            default:
                Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   

    void HandShake()
    {
        //// Example to send a handshake message:
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = m_Connection.InternalId.ToString();
        SendToServer(JsonUtility.ToJson(m));
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}