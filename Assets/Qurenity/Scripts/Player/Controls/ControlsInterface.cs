using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface ControlsInterface
{
	void SetCameraBobActive(bool active);
	bool JumpPressedThisFrame { get; }
	bool JumpReleasedThisFrame { get; }
	bool JumpPressed { get; }
	bool FirePressedThisFrame { get; }
	bool FirePressed { get; }
	void CheckCameraChange();
	void SetViewDirection();
	Vector2 Look { get; }
	Vector3 ForwardDir { get; }
	bool IsControllerGrounded { get; }
	bool CrouchPressedThisFrame { get; }
	bool CrouchReleasedThisFrame { get; }
	void CrouchChangePlayerSpeed(bool Standing);
	void ChangeHeight(bool Standing);
	void CheckIfRunning();
	void QueueJump();
	void CheckMouseWheelWeaponChange();
	void CheckWeaponChangeByIndex();
	void ApplyMove();
	void ApplySimpleGravity();
	void CheckMovements();
	void EnableColliders(bool enable);
	Vector2 GetBobDelta(float hBob, float vBob, float lerp);
}