using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class URLFileLoader : MonoBehaviour
{
	public static URLFileLoader Instance;
	public GameObject StartButton;
	public Text downloadText;

	public string[] displayText = new string[4] { "DOWNLOADING FILES \n", "DOWNLOADING FILES \n.", "DOWNLOADING FILES \n..", "DOWNLOADING FILES \n..." };
	bool downloadReady = false;
	int currentdisplay = 0;

	float time = 2;
	private void Awake()
	{
		Instance = this;
	}
	private void Start()
	{
		string filePath = Application.persistentDataPath + "/pak0.pk3";
		if (File.Exists(filePath))
		{
			downloadReady = true;
			return;
		}
		DownloadFile("https://raw.githubusercontent.com/KNCarnage/idstuff/main/Q3A%20Demo/demoq3/pak0.pk3", filePath);
	}

	public void DownloadFile(string url, string path)
	{
		StartCoroutine(GetFileRequest(url, path, (UnityWebRequest req) =>
		{
			if ((req.result == UnityWebRequest.Result.ConnectionError) || (req.result == UnityWebRequest.Result.ProtocolError))
			{
				// Log any errors that may happen
				Debug.Log(req.error + " : " + req.downloadHandler.text);
			}
			else
			{
				downloadReady = true;
			}
		}));
	}

	IEnumerator GetFileRequest(string url, string path, Action<UnityWebRequest> callback)
	{
		using (UnityWebRequest req = UnityWebRequest.Get(url))
		{
			req.downloadHandler = new DownloadHandlerFile(path);
			yield return req.SendWebRequest();
			callback(req);
		}
	}

	public void StartGame()
	{
		StartButton.SetActive(false);
		SceneManager.LoadScene("Qurenity", LoadSceneMode.Single);
	}

	void Update()
	{
		if (downloadReady)
		{
			if (!StartButton.activeSelf)
			{
				string filePath = Application.streamingAssetsPath + "/pak0.pk3";
				if (!File.Exists(filePath))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(filePath));
					string localPath = Application.persistentDataPath + "/pak0.pk3";
					FileStream stream = File.Open(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					byte[] pakBytes = File.ReadAllBytes(localPath);
					stream.Close();
					stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
					stream.Write(pakBytes, 0, pakBytes.Length);
					stream.Close();
				}
				downloadText.text = "DOWNLOAD READY";
				StartButton.SetActive(true);
			}
			
		}
		else
		{
			time -= Time.deltaTime;
			if (time < 0)
			{
				downloadText.text = displayText[currentdisplay++];
				if (currentdisplay >= displayText.Length)
					currentdisplay = 0;
				time = 2;
			}
		}
	}
}