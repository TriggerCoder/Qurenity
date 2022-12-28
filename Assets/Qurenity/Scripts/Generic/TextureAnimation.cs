using UnityEngine;

public class TextureAnimation : MonoBehaviour
{
	public static readonly string[] texture = { "_S_Texture", "_W_Texture", "_IW_Texture" };

	public float frameTime = 1f;
	public string[] frames;
	private Texture[] _frames;
	public int direction = 1;
	public int textureType;

	MeshRenderer mr;
	MaterialPropertyBlock materialParameters;

	void Awake()
	{
		mr = GetComponent<MeshRenderer>();
		materialParameters = new MaterialPropertyBlock();
	}

	void Start()
	{
		if (frames.Length == 0)
			enabled = false;

		_frames = new Texture[frames.Length];

		for (int i = 0; i < frames.Length; i++)
		{
			_frames[i] = TextureLoader.Instance.GetTexture(frames[i]);
		}

		SetFirstFrame();
		if (frameTime == 0)
			enabled = false;

		frameTime = 1 / frameTime;
	}

	void SetFirstFrame()
	{
		mr.GetPropertyBlock(materialParameters);
		materialParameters.SetTexture(texture[textureType], _frames[0]);
		mr.SetPropertyBlock(materialParameters);
	}
	bool lastflip = false;
	int index = 0;

	void OnEnable()
	{
		lastflip = false;
		index = 0;
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		bool flip = Time.time % (frameTime + .15f) > frameTime;
		if (flip != lastflip)
		{
			lastflip = flip;
			index += direction;

			if (index >= _frames.Length)
					index = 0;

			if (index < 0)
					index = _frames.Length - 1;

			mr.GetPropertyBlock(materialParameters);
			materialParameters.SetTexture(texture[textureType], _frames[index]);
			mr.SetPropertyBlock(materialParameters);
		}
	}
}
