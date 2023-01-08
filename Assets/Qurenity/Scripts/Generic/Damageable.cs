using UnityEngine;

public enum DamageType
{
	Generic,
	Rocket,
	Grenade,
	Plasma,
	Lightning,
	BFGBall,
	BFGBlast,
	Explosion,
	Environment,
	Crusher,
	Telefrag,
	Electric,
}

public enum BloodType
{
	Red,
	Blue,
	Green,
	None
}

public interface Damageable
{
	int Hitpoints { get; }
	bool Dead { get; }
	bool Bleed { get; }
	BloodType BloodColor { get; }
	void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null);
	void Impulse(Vector3 direction, float force);
	void JumpPadDest(Vector3 destination);
}