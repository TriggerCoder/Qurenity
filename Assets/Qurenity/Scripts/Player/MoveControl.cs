using UnityEngine;

public interface MoveControl
{
	Vector2 Move { get; }
	Vector2 View { get; }
	Vector2 Look { get; }
	Vector3 ForwardDir { get; }
	bool CrouchPressedThisFrame { get; }
	bool CrouchReleasedThisFrame { get; }
	void SetCameraOffsetY(float offset);
	void ApplyBobAndCheckFire();
	bool RunReleasedThisFrame { get; }
	bool RunPressed { get; }
	bool JumpPressedThisFrame { get; }
	bool JumpReleasedThisFrame { get; }
	bool JumpPressed { get; }
	void CheckMouseWheel();
	void CheckWeaponChangeByIndex();
	bool IsControllerGrounded { get; }
	void ApplyMove();
	void ApplySimpleGravity();
	void CheckMovements();
	void ChangeColliderHeight(Vector3 center, float height);
	void EnableColliders(bool enable);
}