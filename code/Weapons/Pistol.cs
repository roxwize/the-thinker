using Sandbox;

namespace ThekiFake.TheThinker;

public partial class Pistol : Weapon
{
	public override AmmoType AmmoType => AmmoType.Pistol;

	public override string PrintName => "Pistol";
	public override string ModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Pistol;
	
	[ClientRpc]
	protected void ShootEffects()
	{
		Game.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", this, "muzzle" );

		Pawn.SetAnimParameter( "b_attack", true );
	}

	public override void PrimaryAttack()
	{
		ClipAmmo--;
		ShootEffects();
		Pawn.PlaySound( "rust_pistol.shoot" );
		ShootBullet( 0.1f, 100, 20, 1 );
	}

	protected void ReloadEffects()
	{
		
	}
}
