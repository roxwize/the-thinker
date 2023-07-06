using System;
using System.Collections.Generic;
using Sandbox;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Sandbox.Services;

namespace ThekiFake.TheThinker;

public partial class ThinkerPlayer : AnimatedEntity
{
	[ClientInput]
	public Vector3 InputDirection { get; set; }
	[ClientInput]
	public Angles ViewAngles { get; set; }

	[Browsable(false)]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}
	[Net, Predicted, Browsable(false)]
	public Vector3 EyeLocalPosition { get; set; }
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}
	[Net, Predicted, Browsable(false)]
	public Rotation EyeLocalRotation { get; set; }

	public BBox Hull => new(
		new Vector3( -16, -16, 0 ),
		new Vector3( 16, 16, 64 )
	);

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	[Net, Predicted]
	public Weapon ActiveWeapon { get; protected set; }

	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Static, Hull.Mins, Hull.Maxs );
		
		EnableHitboxes = true;
		
		Tags.Add( "player" );
	}

	public void Respawn( IClient cl )
	{
		Components.Create<PawnController>();
		Components.Create<PawnAnimator>();
		Ammo = new AmmoContainer();

		DressFromClient( cl );
		Health = 100;
		
		var spawnpoints = All.OfType<SpawnPoint>();

		var randomSpawnPoint = spawnpoints.MinBy(_ => Guid.NewGuid());

		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position += Vector3.Up * 50.0f; // raise it up
			Transform = tx;
		}

		EnableDrawing = true;
		EnableAllCollisions = true;
		Velocity = Vector3.Zero;
		Corpse = null;
		
		EquipWeapon( new Pistol() );
	}

	[Net] public Entity LastAttackerEntity { get; set; }
	public ModelEntity Corpse;
	[Net, Predicted] private TimeSince TimeSinceDied { get; set; }

	public override void TakeDamage( DamageInfo info )
	{
		LastAttackerEntity = info.Attacker;
		
		if ( info.Hitbox.HasTag( "head" ) )
		{
			info.Damage *= 3;
		}
		
		base.TakeDamage( info );
	}

	public override void OnKilled()
	{
		GameManager.Current?.OnKilled( this );
		
		TimeSinceDied = 0;
		LifeState = LifeState.Dead;
		BecomeRagdollOnClient( Velocity );
		Components.Remove( Controller );
		PlaySound( "death" );
		
		if ( LastAttackerEntity != null && LastAttackerEntity.IsValid && Game.IsServer ) OnKillPlayer( To.Single( LastAttackerEntity.Client ) );

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children )
		{
			child.EnableDrawing = false;
		}
	}
	
	[ClientRpc]
	private void OnKillPlayer()
	{
		Stats.Increment( "kills", 1 );
	}

	[ClientRpc]
	private void BecomeRagdollOnClient( Vector3 velocity )
	{
		var ent = new ModelEntity();
		ent.Tags.Add( "ragdoll" );
		ent.Position = Position;
		ent.Rotation = Rotation;
		ent.UsePhysicsCollision = true;
		ent.EnableAllCollisions = true;
		ent.SetModel( GetModelName() );
		ent.CopyBonesFrom( this );
		ent.CopyBodyGroups( this );
		ent.CopyMaterialGroup( this );
		ent.CopyMaterialOverrides( this );
		ent.TakeDecalsFrom( this );
		ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;
		ent.PhysicsGroup.Velocity = velocity;
		ent.PhysicsEnabled = true;

		foreach ( var child in Children )
		{
			if ( !child.Tags.Has( "clothes" ) ) continue;
			if ( child is not ModelEntity e ) continue;

			var model = e.GetModelName();

			var clothing = new ModelEntity();
			clothing.SetModel( model );
			clothing.SetParent( ent, true );
			clothing.CopyBodyGroups( e );
			clothing.CopyMaterialGroup( e );
		}

		Corpse = ent;

		ent.DeleteAsync( TheThinkerGame.RespawnTime );
	}

	[ConCmd.Admin("spawn_box")]
	public static void SpawnBox()
	{
		var player = (ThinkerPlayer)ConsoleSystem.Caller.Pawn;
		
		var box = new Prop
		{
			Position = player.Position + player.Rotation.Forward * 100
		};
		box.SetModel( "models/citizen_props/cardboardbox01.vmdl" );
	}
	
	[ConCmd.Admin("givecurrentammo")]
	public static void GiveAmmo()
	{
		var player = (ThinkerPlayer)ConsoleSystem.Caller.Pawn;

		player.Ammo.Set( player.ActiveWeapon.AmmoType, Int32.MaxValue );
	}

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;
		if ( Input.StopProcessing ) return;

		var look = Input.AnalogLook;
		
		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89, 89 );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;
	}
	
	[BindComponent] public PawnController Controller { get; }
	[BindComponent] public PawnAnimator Animator { get; }
	[Net] public AmmoContainer Ammo { get; set; } = new();

	public override void Simulate( IClient cl )
	{
		if ( LifeState == LifeState.Dead && TimeSinceDied > TheThinkerGame.RespawnTime )
		{
			LifeState = LifeState.Alive;
			Respawn( cl );
		}
		
		SimulateRotation();
		Controller?.Simulate( cl );
		Animator?.Simulate();
		ActiveWeapon?.Simulate( cl );

		EyeLocalPosition = Vector3.Up * (64 * Scale);
	}

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();
		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
		Camera.FirstPersonViewer = null;

		if ( LifeState != LifeState.Dead )
		{
			Vector3 targetPos;
			var pos = Position + Vector3.Up * 64;
			var rot = Camera.Rotation;

			var distance = 20 * Scale;
			targetPos = pos + rot.Left * ((CollisionBounds.Mins.x + 30) * Scale);
			targetPos += rot.Forward * -distance;

			var tr = Trace.Ray( pos, targetPos )
				.WithAnyTags( "solid", "player" )
				.Ignore( this )
				.Radius( 8 )
				.Run();
			
			Camera.Position = tr.EndPosition;
		}
		else
		{
			if ( LastAttackerEntity != null )
			{
				Camera.Position = LastAttackerEntity.Position + LastAttackerEntity.Rotation.Backward * 100 + Vector3.Up * 50;
				Camera.Rotation = LastAttackerEntity.Rotation;
			}
			else
			{
				Camera.Position = Corpse.Position + Vector3.Up * 50;
			}
		}
	}

	public void DressFromClient( IClient cl )
	{
		var c = new ClothingContainer();
		c.LoadFromClient( cl );
		c.DressEntity( this );
	}

	protected void SimulateRotation()
	{
		EyeRotation = ViewAngles.ToRotation();
		Rotation = ViewAngles.WithPitch( 0f ).ToRotation();
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0 )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0 )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		return Trace.Ray( start, end )
			.Size( mins, maxs )
			.WithAnyTags( "solid", "playerclip" )
			.Ignore( this )
			.Run();
	}

	public void EquipWeapon( Weapon weapon )
	{
		ActiveWeapon?.OnHolster();
		ActiveWeapon = weapon;
		weapon.OnEquip( this );
	}
}
