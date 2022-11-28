using UnityEngine;
using UnityEngine.Rendering;
public class SkyBoxCamera : MonoBehaviour
{

	public RenderTexture background;

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination);
		Graphics.Blit(source, background);
	}
}
