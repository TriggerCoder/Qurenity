using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAnimation : MonoBehaviour
{
	public string modelName;
	public bool isTransparent;

	private MD3 md3Model;

	private MD3UnityConverted unityModel;

	[SerializeField]
	public AnimData modelAnimation;
	[SerializeField]
	public AnimData textureAnimation;

	private List<int> modelAnim = new List<int>();
	private List<int> textureAnim = new List<int>();
	private Dictionary<int, Texture[]> textures = new Dictionary<int, Texture[]>();

	private int modelCurrentFrame;
	private List<int> textureCurrentFrame = new List<int>();

	[System.Serializable]
	public struct AnimData
	{
		public float fps;
		public float lerpTime;
		public float currentLerpTime;
	}
	void Awake()
	{
		if (string.IsNullOrEmpty(modelName))
		{
			Destroy(gameObject);
			return;
		}
	}

	void Start()
	{
		md3Model = ModelsManager.GetModel(modelName, isTransparent);
		if (md3Model == null)
		{
			enabled = false;
			return;
		}
		if (md3Model.readyMeshes.Count == 0)
			unityModel = Mesher.GenerateModelFromMeshes(md3Model, gameObject, isTransparent);
		else
			unityModel = Mesher.FillModelFromProcessedData(md3Model, gameObject);

		for (int i = 0; i < md3Model.meshes.Count; i++)
		{
			var modelMesh = md3Model.meshes[i];
			if (modelMesh.numFrames > 1)
				modelAnim.Add(i);
			if (modelMesh.numSkins > 1)
			{
				Texture[] frames = new Texture[modelMesh.numSkins];
				for (int j = 0; j < modelMesh.numSkins; j++)
				{
					string texName = modelMesh.skins[j].name;
					if (TextureLoader.HasTexture(texName))
						frames[j] = TextureLoader.Instance.GetTexture(texName);
					else
					{
						TextureLoader.AddNewTexture(texName, isTransparent);
						frames[j] = TextureLoader.Instance.GetTexture(texName);
					}
				}
				textureAnim.Add(i);
				textureCurrentFrame.Add(0);
				textures.Add(i, frames);
			}
		}

		//If no vertex animation nor texture animation, disable the animation
		if ((modelAnim.Count == 0) && (textureAnim.Count == 0))
			enabled = false;
	}

	void OnEnable()
	{
		modelCurrentFrame = 0;
		modelAnimation.currentLerpTime = 0;
		for(int i = 0; i < textureCurrentFrame.Count; i++)
			textureCurrentFrame[i] = 0;
		textureAnimation.currentLerpTime = 0;
	}

	void AnimateModel()
	{
		int currentFrame = modelCurrentFrame;
		int nextFrame = currentFrame + 1;
		float t = modelAnimation.currentLerpTime;
		if (nextFrame >= md3Model.numFrames)
			nextFrame = 0;

		for (int i = 0; i < modelAnim.Count; i++)
		{
			MD3Mesh currentMesh = md3Model.meshes[modelAnim[i]];
			List<Vector3> lerpVertex = new List<Vector3>(currentMesh.numVertices);
			for (int j = 0; j < currentMesh.numVertices; j++)
			{
				Vector3 newVertex = Vector3.Lerp(currentMesh.verts[currentFrame][j], currentMesh.verts[nextFrame][j], t);
				lerpVertex.Add(newVertex);
			}
			unityModel.data[i].meshFilter.mesh.SetVertices(lerpVertex);
		}

		modelAnimation.lerpTime = modelAnimation.fps * Time.deltaTime;
		modelAnimation.currentLerpTime += modelAnimation.lerpTime;

		if (modelAnimation.currentLerpTime >= 1.0f)
		{
			modelAnimation.currentLerpTime -= 1.0f;
			modelCurrentFrame = nextFrame;
		}
	}

	void AnimateTexture()
	{
		textureAnimation.lerpTime = textureAnimation.fps * Time.deltaTime;
		textureAnimation.currentLerpTime += textureAnimation.lerpTime;

		for (int i = 0; i < textureAnim.Count; i++)
		{
			MD3Mesh currentMesh = md3Model.meshes[textureAnim[i]];

			int currentFrame = textureCurrentFrame[i];
			int nextFrame = currentFrame + 1;
			if (nextFrame >= currentMesh.numSkins)
				nextFrame = 0;
			if (textureAnimation.currentLerpTime >= 1.0f)
			{
				unityModel.data[currentMesh.meshNum].meshRenderer.material.mainTexture = textures[i][nextFrame];
				textureCurrentFrame[i] = nextFrame;
			}
		}

		if (textureAnimation.currentLerpTime >= 1.0f)
			textureAnimation.currentLerpTime -= 1.0f;
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		AnimateModel();
		AnimateTexture();
	}
}
