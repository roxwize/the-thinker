using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ThekiFake.TheThinker;

public class HealthBar : Panel
{
	public Label Icon;
	public Label HealthLabel;

	public TimeUntil TimeUntilRespawn;
	private bool alive = false; // could do this with LifeState.Dying, but i dont feel like it lawl
	
	public static HealthBar Current;
	
	public HealthBar()
	{
		Current = this;
		Icon = Add.Label( "add", "icon" );
		HealthLabel = Add.Label( "0%" );
	}
	
	public override void Tick()
	{
		var player = Game.LocalPawn as ThinkerPlayer;
		if ( player == null ) return;

		if ( player.LifeState == LifeState.Alive )
		{
			if ( !alive )
			{
				AddClass( "alive" );
				RemoveClass( "dead" );
				alive = true;
			}
			
			Icon.Text = "add";
			var hp = player.Health.CeilToInt();

			HealthLabel.Text = $"{hp}%";
			Style.Width = Length.Percent( hp );
		}
		else
		{
			if ( alive )
			{
				TimeUntilRespawn = TheThinkerGame.RespawnTime;
				RemoveClass( "alive" );
				AddClass( "dead" );
				alive = false;
			}
			
			Icon.Text = "timer";
			HealthLabel.Text = $"{TimeUntilRespawn.Relative.CeilToInt()}";
			Style.Width = Length.Percent( TimeUntilRespawn.Fraction * 100 );
		}
	}
}
