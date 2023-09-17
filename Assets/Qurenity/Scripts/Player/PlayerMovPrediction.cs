using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovPrediction	
{
	public float DistanceTolerance = 0.02f;						//The amount of distance in units that we will allow the client's prediction to drift from it's position on the server, before a correction is necessary. 
	public float SnapDistance = 2f;								//The amount of distance in units when we just snap to the server position.
	public PlayerControls.PlayerState serverSimulationState;   //Latest simulation state from the server

	public const int STATE_CACHE_SIZE = 256;
	public PlayerControls.PlayerInputs[] inputStateCache = new PlayerControls.PlayerInputs[STATE_CACHE_SIZE];
	public PlayerControls.PlayerState[] simulationStateCache = new PlayerControls.PlayerState[STATE_CACHE_SIZE];

	public Vector3 MovementDirection;
	public PlayerMovPrediction()
	{
		for (int i = 0; i < STATE_CACHE_SIZE; i++)
		{
			simulationStateCache[i] = new PlayerControls.PlayerState();
			inputStateCache[i] = new PlayerControls.PlayerInputs();
		}
	}
}
