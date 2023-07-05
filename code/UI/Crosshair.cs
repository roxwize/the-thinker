using Sandbox;
using Sandbox.UI;

namespace ThekiFake.TheThinker;

[StyleSheet]
public class Crosshair : Panel
{
	ThinkerPlayer Owner = Game.LocalPawn as ThinkerPlayer;
	private static float RootScaleFromScreen => Game.RootPanel.ScaleFromScreen;

	private static readonly Color TargetColor = new Color( 255, 0, 0, 0.8f );
	private static readonly Color DefaultColor = new Color( 255, 255, 255, 0.4f );
	
	[GameEvent.Tick]
	public override void Tick()
	{
		if (Owner == null) return;
		if ( Owner.LifeState == LifeState.Dead )
		{
			Style.BackgroundColor = Color.Transparent;
			return;
		}
		
		var tr = Trace.Ray( Owner.AimRay, 5000f )
			.Ignore( Owner )
			.Run();

		if ( tr.Hit && tr.Entity != null && tr.Entity.Tags.Has( "player" ) ) Style.BackgroundColor = TargetColor;
		else Style.BackgroundColor = DefaultColor;
		
		var pos = tr.EndPosition.ToScreen();
		
		pos.x *= Screen.Width;
		pos.y *= Screen.Height;
		pos.x -= Box.Rect.Width / 2;
		pos.y -= Box.Rect.Height / 2;
		pos.x *= RootScaleFromScreen;
		pos.y *= RootScaleFromScreen;

		Style.Left = Length.Pixels( pos.x );
		Style.Top = Length.Pixels( pos.y );
	}
}
