using Sandbox;
using System;

namespace ThekiFake.TheThinker;

public class PawnAnimator : EntityComponent<ThinkerPlayer>, ISingletonComponent
{
	public void Simulate()
	{
		if ( Entity.Controller == null ) return;
		var helper = new CitizenAnimationHelper( Entity );
		helper.WithVelocity( Entity.Velocity );
		helper.WithLookAt( Entity.EyePosition + Entity.EyeRotation.Forward * 100 );
		helper.HoldType = Entity.ActiveWeapon?.HoldType ?? CitizenAnimationHelper.HoldTypes.None;
		helper.IsGrounded = Entity.GroundEntity.IsValid();

		if ( Entity.Controller.HasEvent( "jump" ) )
		{
			helper.TriggerJump();
		}
	}
}
