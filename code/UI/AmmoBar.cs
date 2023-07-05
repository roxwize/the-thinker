using System;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ThekiFake.TheThinker;

public class AmmoBar : Panel
{
	public Label AmmoLabel;
	
	public AmmoBar()
	{
		_ = Add.Label( "backpack", "icon" );
		AmmoLabel = Add.Label( "0/0" );
	}
	
	public override void Tick()
	{
		var player = Game.LocalPawn as ThinkerPlayer;
		if ( player == null ) return;
		var active = player.ActiveWeapon;
		var percent = ((float)active.ClipAmmo / active.ClipSize) * 100f;

		AmmoLabel.Text = $"{active.ClipAmmo}/{player.Ammo.Get( player.ActiveWeapon.AmmoType )}";
		Style.Width = Length.Percent( percent.CeilToInt() );
	}
}
