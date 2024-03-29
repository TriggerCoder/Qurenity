using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAnimation : MonoBehaviour
{
	public string modelName;
	public bool isTransparent = false;
	public bool castShadow = true;

	private MD3 md3Model;

	private MD3UnityConverted unityModel;

	[SerializeField]
	public AnimData modelAnimation;
	[SerializeField]
	public AnimData textureAnimation;
	[SerializeField]
	public PosAnim localAnimation;

	private List<int> modelAnim = new List<int>();
	private List<int> textureAnim = new List<int>();
	private Dictionary<int, Texture[]> textures = new Dictionary<int, Texture[]>();

	private int modelCurrentFrame;
	private List<int> textureCurrentFrame = new List<int>();

	private Vector3 currentOrigin;
	private float height;
	private float timer = 0;
	private Transform cTransform;

	[System.Serializable]
	public struct AnimData
	{
		public float fps;
		public float lerpTime;
		public float currentLerpTime;
	}

	[System.Serializable]
	public struct PosAnim
	{
		public bool rotEnable;
		public float rotFPS;
		public bool rotClockwise;

		public bool posEnable;
		public float posAmplitude;
		public float posFPS;
	}
	void Awake()
	{
		if (string.IsNullOrEmpty(modelName))
		{
			Destroy(gameObject);
			return;
		}

		md3Model = ModelsManager.GetModel(modelName, isTransparent);
		if (md3Model == null)
		{
			Debug.LogWarning("Model not found: " + modelName);
			enabled = false;
			return;
		}
		GameObject currentObject;
		//If There are other ModelAnimation, add this as a child
		if (GetComponent<MeshRenderer>() != null)
		{
			currentObject = new GameObject(modelName);
			currentObject.transform.SetParent(transform);
			currentObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		}
		else
			currentObject = gameObject;

		if (md3Model.readyMeshes.Count == 0)
			unityModel = Mesher.GenerateModelFromMeshes(md3Model, currentObject, isTransparent, null, (modelAnim.Count > 0));
		else
			unityModel = Mesher.FillModelFromProcessedData(md3Model, currentObject);

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
		cTransform = currentObject.transform;
	}

	void Start()
	{
		//If no vertex animation nor texture animation, disable the animation
		if ((modelAnim.Count == 0) && (textureAnim.Count == 0) && (!localAnimation.rotEnable) && (!localAnimation.posEnable))
			enabled = false;

		//Transparent models never cast shadow
		if ((isTransparent) || (!castShadow))
			for (int i = 0; i < md3Model.readyMeshes.Count; i++)
				unityModel.data[i].meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

		currentOrigin = cTransform.position;
		height = currentOrigin.y;
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
			unityModel.data[i].meshFilter.mesh.RecalculateNormals();
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

	void RotateModel()
	{
		if (!localAnimation.rotEnable)
			return;

		cTransform.RotateAround(currentOrigin, localAnimation.rotClockwise? Vector3.up : Vector3.down, Time.deltaTime * localAnimation.rotFPS);
	}

	void MoveModel()
	{
		if (!localAnimation.posEnable)
			return;

		timer += Time.deltaTime * localAnimation.posFPS;
		float offSet = localAnimation.posAmplitude * Mathf.Sin(timer) + height;
		currentOrigin = new Vector3(currentOrigin.x, offSet, currentOrigin.z);
		cTransform.position = currentOrigin;
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		RotateModel();
		MoveModel();
		AnimateModel();
		AnimateTexture();
	}
}
