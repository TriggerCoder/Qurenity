using Riptide;
using Riptide.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ClientToServerId : ushort
{
	name = 1
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
	void SendName()
	{
		if (isServer)
			return;

		Message msg = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);
		msg.AddString("Nombre");
		client.Send(msg);
	}
}
