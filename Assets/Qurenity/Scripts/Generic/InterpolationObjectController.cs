using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(ORDER_EXECUTION)]
public class InterpolationObjectController : MonoBehaviour
{
	public const int ORDER_EXECUTION = InterpolationFactorManager.ORDER_EXECUTION - 1;

	private TransformData[] transforms;
	private int index;
	private Transform cTransform;

	void Awake()
	{
		cTransform = transform;
		StartCoroutine(WaitForEndOfFrame());
		StartCoroutine(WaitForFixedUpdate());
	}

	void OnEnable()
	{
		ResetTransforms();
	}

	void BeforeFixedUpdate()
	{
		// Restoring actual transform for the FixedUpdate() cal where it could be change by the user.
		RestoreActualTransform();
	}

	void AfterFixedUpdate()
	{
		// Saving actual transform for being restored in the BeforeFixedUpdate() method.
		SaveActualTransform();
	}

	void Update()
	{
		// Set interpolated transform for being rendered.
		SetInterpolatedTransform();
	}

	void RestoreActualTransform()
	{
		var latest = transforms[index];
		cTransform.localPosition = latest.position;
		cTransform.localScale = latest.scale;
		cTransform.localRotation = latest.rotation;
	}

	void SaveActualTransform()
	{
		index = NextIndex();
		transforms[index] = CurrentTransformData();
	}

	void SetInterpolatedTransform()
	{
		var prev = transforms[NextIndex()];
		float factor = InterpolationFactorManager.Factor;
		cTransform.localPosition = Vector3.Lerp(prev.position, cTransform.localPosition, factor);
		cTransform.localRotation = Quaternion.Slerp(prev.rotation, cTransform.localRotation, factor);
		cTransform.localScale = Vector3.Lerp(prev.scale, cTransform.localScale, factor);
	}

	public void ResetTransforms()
	{
		index = 0;
		var td = CurrentTransformData();
		transforms = new TransformData[2] { td, td };
	}

	private TransformData CurrentTransformData()
	{
		return new TransformData(cTransform.localPosition, cTransform.localRotation, cTransform.localScale);
	}

	int NextIndex()
	{
		return (index == 0) ? 1 : 0;
	}

	private IEnumerator WaitForEndOfFrame()
	{
		while (true)
		{
			yield return new WaitForEndOfFrame();
			BeforeFixedUpdate();
		}
	}

	private IEnumerator WaitForFixedUpdate()
	{
		while (true)
		{
			yield return new WaitForFixedUpdate();
			AfterFixedUpdate();
		}
	}

	private struct TransformData
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		public TransformData(Vector3 position, Quaternion rotation, Vector3 scale)
		{
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
		}
	}
}
