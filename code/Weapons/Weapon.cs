using Sandbox;
using System.Collections.Generic;

namespace ThekiFake.TheThinker;

public enum AmmoType
{
	None = -1,
	Pistol = 0
}

public partial class Weapon : AnimatedEntity
{
	// FIRING
	public virtual float PrimaryRate => 5.0f;
	public virtual int ClipSize => 12;
	[Net, Predicted] public int ClipAmmo { get; set; }
	public virtual AmmoType AmmoType => AmmoType.None;
	[Net, Predicted] public TimeSince TimeSincePrimaryAttack { get; set; }

	// RELOADING
	public virtual float ReloadTime => 1.5f;
	public bool IsReloading;
	[Net, Predicted] public TimeSince TimeSinceReload { get; set; }

	// VISUALS
	public virtual string PrintName => null;
	public virtual string[] KillFeedDescriptions { get; } = new[]
	{
		"made a man out of",
		"made an example of",
		"disciplined",
		"murdered",
		"eviscerated",
		"0wned",
		"fucked up",
		"shot the shit out of",
		"discombobulated",
		"deleted",
		"is better than"
	};
	public virtual string ModelPath => null;
	public virtual CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.None;
	
	// MISC
	public ThinkerPlayer Pawn => Owner as ThinkerPlayer;
	public int Ammo => Pawn.Ammo.Get( AmmoType );
	public bool WantsReload;

	public override void Spawn()
	{
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableDrawing = false;

		ClipAmmo = ClipSize;

		if ( ModelPath != null )
		{
			SetModel( ModelPath );
		}
	}

	/// <summary>
	/// Called when <see cref="ThinkerPlayer.EquipWeapon(Weapon)"/> is called for this weapon.
	/// </summary>
	/// <param name="pawn"></param>
	public void OnEquip( ThinkerPlayer pawn )
	{
		Owner = pawn;
		SetParent( pawn, true );
		EnableDrawing = true;
	}

	/// <summary>
	/// Called when the weapon is either removed from the player, or holstered.
	/// </summary>
	public void OnHolster()
	{
		EnableDrawing = false;
	}

	/// <summary>
	/// Called from <see cref="ThinkerPlayer.Simulate(IClient)"/>.
	/// </summary>
	/// <param name="player"></param>
	public override void Simulate( IClient player )
	{
		Animate();

		if ( CanReload() )
		{
			Reload();
		}
		
		if ( IsReloading && TimeSinceReload > ReloadTime ) OnReloadEnd();

		if ( CanPrimaryAttack() )
		{
			using ( LagCompensation() )
			{
				TimeSincePrimaryAttack = 0;
				PrimaryAttack();
			}
		}
	}

	/// <summary>
	/// Called every <see cref="Simulate(IClient)"/> to see if we can shoot our gun.
	/// </summary>
	/// <returns></returns>
	public virtual bool CanPrimaryAttack()
	{
		if ( !Owner.IsValid() || !Input.Down( "attack1" ) || IsReloading || Owner.LifeState != LifeState.Alive ) return false;
		if ( ClipAmmo <= 0 )
		{
			if ( Input.Pressed( "attack1" ) )
			{
				if ( Ammo > 0 ) WantsReload = true;
				else PlaySound( "click-empty" );
			}
			return false;
		}

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	/// <summary>
	/// Called when your gun shoots.
	/// </summary>
	public virtual void PrimaryAttack()
	{
	}

	public virtual bool CanReload()
	{
		if ( WantsReload ) return true; // shitty fix but whatever
		if ( !Owner.IsValid() || !Input.Down( "reload" ) || Ammo == 0 || ClipAmmo >= ClipSize || Owner.LifeState != LifeState.Alive ) return false;
		return true;
	}

	public virtual void Reload()
	{
		if ( IsReloading ) return;

		TimeSinceReload = 0;
		IsReloading = true;
		WantsReload = false;
		
		Pawn.SetAnimParameter( "b_reload", true );
	}

	public virtual void OnReloadEnd()
	{
		IsReloading = false;
		var primaryAmmo = Pawn.Ammo.Get( AmmoType );
		if ( primaryAmmo > 0 )
		{
			var remaining = ClipSize - ClipAmmo;
			if ( remaining > 0 )
			{
				var toTake = remaining < primaryAmmo ? remaining : primaryAmmo;
				ClipAmmo += toTake;
				Pawn.Ammo.Set( AmmoType, Pawn.Ammo.Get( AmmoType ) - toTake );
			}
		}
	}

	/// <summary>
	/// Useful for setting anim parameters based off the current weapon.
	/// </summary>
	protected virtual void Animate()
	{
	}

	/// <summary>
	/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	/// hits, like if you're going through layers or ricocheting or something.
	/// </summary>
	public virtual IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool underWater = Trace.TestPoint( start, "water" );

		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc" )
				.Ignore( this )
				.Size( radius );

		//
		// If we're not underwater then we can hit water
		//
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;
	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize )
	{
		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( pos, pos + forward * 5000, bulletSize ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !Game.IsServer ) return;
			if ( !tr.Entity.IsValid() ) continue;

			//
			// We turn prediction off for this, so any exploding effects don't get culled etc
			//
			using ( Prediction.Off() )
			{
				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100 * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		Game.SetRandomSeed( Time.Tick );

		var ray = Owner.AimRay;
		ShootBullet( ray.Position, ray.Forward, spread, force, damage, bulletSize );
	}
}
