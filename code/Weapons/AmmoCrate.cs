using System.Collections.Generic;
using Sandbox;

namespace ThekiFake.TheThinker;

public class AmmoCrate : BaseTrigger
{
	public IList<int> Ammo { get; protected set; }

	public AmmoCrate() : this( new List<int>() )
	{
		
	}
	
	public AmmoCrate( IList<int> items )
	{
		Ammo = items;
	}

	public override void Spawn()
	{
		base.Spawn();

		EnableDrawing = true;
		EnableTouch = true;
	}

	public override void StartTouch( Entity other )
	{
		Log.Info( other.Name );
		if ( !Game.IsServer ) return;
		if ( other is not ThinkerPlayer p ) return;
		p.Ammo.AddTo( Ammo );
		Delete();
	}
}
