using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ORDER_EXECUTION)]
public class InterpolationFactorManager : MonoBehaviour
{
	public const int ORDER_EXECUTION = -1000;
	private static InterpolationFactorManager Instance;
	private float[] lastFixedUpdates = new float[2];
	private int lastIndex;

	public static float Factor { get; private set; }

	private void Awake()
	{
		Instance = this;
		Factor = 1;
	}

	private void Start()
	{
		lastFixedUpdates = new float[2] { Time.fixedTime, Time.fixedTime };
		lastIndex = 0;
	}

	private void FixedUpdate()
	{
		lastIndex = NextIndex();
		lastFixedUpdates[lastIndex] = Time.fixedTime;
	}

	private void Update()
	{
		float lastTime = lastFixedUpdates[lastIndex];
		float prevTime = lastFixedUpdates[NextIndex()];

		if (lastTime == prevTime)
		{
			Factor = 1;
			return;
		}

		Factor = (Time.time - lastTime) / (lastTime - prevTime);
	}

	private int NextIndex()
	{
		return (lastIndex == 0) ? 1 : 0;
	}
}
