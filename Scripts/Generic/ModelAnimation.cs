using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAnimation : MonoBehaviour
{
	public string modelName;

	private MD3 md3Model;
	private Mesh modelMesh;
	private Material material;
	public Texture[] _frames;

	public float frameTime = 1f;
	public bool oscillates;
	public int direction = 1;

	private int numSkins;
	private int numFrames;

	bool lastflip = false;
	int currentFrame = 0;
	int currentSkin;
	void Awake()
	{
		if (string.IsNullOrEmpty(modelName))
		{
			enabled = false;
			return;
		}
		md3Model = ModelsManager.GetModel(modelName,true);
		if (md3Model == null)
		{
			enabled = false;
			return;
		}

		modelMesh = Mesher.GenerateModelObjectGetMesh(md3Model, gameObject);
		//Check for differens in skin count per mesh
		numSkins = md3Model.numSkins;
		_frames = new Texture[numSkins];

		numFrames = md3Model.meshes[0].numFrames;

		for (int i = 0; i < _frames.Length; i++)
		{
			string texName = md3Model.meshes[0].skins[i].name;
			if (TextureLoader.HasTexture(texName))
				_frames[i] = TextureLoader.Instance.GetTexture(texName);
			else
			{
				TextureLoader.AddNewTexture(texName,true);
				_frames[i] = TextureLoader.Instance.GetTexture(texName);
			}
		}

		material = GetComponent<MeshRenderer>().material;
	}

	void OnEnable()
	{
		lastflip = false;
		currentFrame = 0;
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		bool flip = Time.time % (frameTime + .15f) > frameTime;
		if (flip != lastflip)
		{
			lastflip = flip;
			currentFrame += direction;

			if (currentFrame >= _frames.Length)
				if (oscillates)
				{
					direction = -1;
					currentFrame--;
				}
				else
					currentFrame = 0;

			if (currentFrame < 0)
				if (oscillates)
				{
					direction = 1;
					currentFrame++;
				}
				else
					currentFrame = _frames.Length - 1;

			if ((currentFrame + 1 % 2) == 0)
				currentSkin++;
			if (currentSkin >= numSkins)
				currentSkin = 0;

			modelMesh.SetVertices(md3Model.meshes[0].verts[currentFrame]);
			material.mainTexture = _frames[currentSkin];
		}
	}
}
