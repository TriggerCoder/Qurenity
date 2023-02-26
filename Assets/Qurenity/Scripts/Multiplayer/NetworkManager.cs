using Riptide;
using Riptide.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ServerToClientId : ushort
{
	playerSpawned = 1,
	playerMovement,
}

public enum ClientToServerId : ushort
{
	name = 1,
	input,
}
public class NetworkManager : MonoBehaviour
{
	public bool isServer = true;
	public static NetworkManager Instance;
	public Server server;
	public Client client;
	[SerializeField]
	public string ip = "127.0.0.1";
	public ushort port = 27960;
	public ushort maxClientCount = 32;
	void Awake()
	{
		Instance = this;
	}

	void Start()
	{
		RiptideLogger.Initialize(Debug.Log, true);
		if (isServer)
		{
			server = new Server();
			server.Start(port, maxClientCount);
			server.ClientDisconnected += PlayerLeft;
		}
		else
		{
			client = new Client();
			client.Connected += DidConnect;
			client.ConnectionFailed += FailedToConnect;
			client.Disconnected += DidDisconnect;
			client.Connect(ip + ":" + port);
		}
	}

	void FixedUpdate()
	{
		if (isServer)
			server.Update();
		else
			client.Update();
	}

	void OnApplicationQuit()
	{
		if (isServer)
			server.Stop();
		else
			client.Disconnect();
	}

	void DidConnect(object sender, EventArgs a)
	{
		if (isServer)
			return;
		SendName();
	}

	void FailedToConnect(object sender, EventArgs a)
	{
		if (isServer)
			return;
	}
	void DidDisconnect(object sender, EventArgs a)
	{
		if (isServer)
			return;
	}
	void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
	{
		if (!isServer)
			return;
		if (GameManager.playerList.TryGetValue(e.Client.Id, out PlayerThing player))
			Destroy(player.gameObject);
	}
	void SendName()
	{
		if (isServer)
			return;

		Message msg = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);
		msg.AddString("Nombre");
		client.Send(msg);
	}
	public void SendSpawned(PlayerThing otherPlayer)
	{
		server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.Reliable, ServerToClientId.playerSpawned), otherPlayer));
	}
	private void SendSpawned(ushort toClientId, PlayerThing otherPlayer)
	{
		server.Send(AddSpawnData(Message.Create(MessageSendMode.Reliable, ServerToClientId.playerSpawned), otherPlayer), toClientId);
	}

	private Message AddSpawnData(Message message, PlayerThing otherPlayer)
	{
		message.AddUShort(otherPlayer.playerId);
		message.AddString(otherPlayer.playerName);
		message.AddVector3(otherPlayer.transform.position);
		return message;
	}

	[MessageHandler((ushort)ClientToServerId.name)]
	private static void Name(ushort fromClientId, Message message)
	{
		GameManager.SpawnPlayer(fromClientId, message.GetString());
		foreach (PlayerThing otherPlayer in GameManager.playerList.Values)
			Instance.SendSpawned(fromClientId, otherPlayer);
	}

	[MessageHandler((ushort)ClientToServerId.input)]
	private static void Input(ushort fromClientId, Message message)
	{
		if (GameManager.playerList.TryGetValue(fromClientId, out PlayerThing player))
			player.playerControls.SetInput(message.GetBools(6), message.GetVector3());
	}

	[MessageHandler((ushort)ServerToClientId.playerMovement)]
	private static void PlayerMovement(Message message)
	{
		if (GameManager.playerList.TryGetValue(message.GetUShort(), out PlayerThing player))
			player.playerControls.SetMove(message.GetVector3(), message.GetVector3());
	}
}
