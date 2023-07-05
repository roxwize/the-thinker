using System;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ThekiFake.TheThinker;

public class KillFeed : Panel
{
	public static KillFeed Current;

	public KillFeed()
	{
		Current = this;
	}

	public Panel AddEntry( long leftid, string left, long rightid, string right, string method, Weapon weapon )
	{
		var e = Current.AddChild<KillFeedEntry>();

		var rand = new Random();

		e.Left.Text = left;
		if ( rightid != 0 ) e.Right.Text = right;
		else e.Right.Delete( true );

		e.Method.Text = weapon != null ? weapon.KillFeedDescriptions[rand.Next(0, weapon.KillFeedDescriptions.Length)] : method;

		return e;
	}
}

public class KillFeedEntry : Panel
{
	public Label Left { get; }
	public Label Right { get; }
	public Label Method { get; }
	private TimeSince TimeSinceCreated;

	public KillFeedEntry()
	{
		Left = Add.Label( "", "killfeed-left" );
		Method = Add.Label( "", "killfeed-method" );
		Right = Add.Label( "", "killfeed-right" );
		AddClass( "box" );
		TimeSinceCreated = 0;
	}

	public override void Tick()
	{
		base.Tick();

		if ( TimeSinceCreated > 5 )
		{
			Delete();
		}
	}
}
