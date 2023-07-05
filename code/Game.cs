using Sandbox;
using System;
using System.Linq;

namespace ThekiFake.TheThinker;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class TheThinkerGame : GameManager
{
	[ConVar.Replicated("tt_respawntime")]
	public static float RespawnTime { get; set; } = 5.0f;
	
	public TheThinkerGame()
	{
		if ( Game.IsClient )
		{
			Game.RootPanel = new Hud();
		}
	}
	
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new ThinkerPlayer();
		client.Pawn = pawn;
		pawn.Respawn( client );
	}

	public override void OnKilled( IClient client, Entity pawn )
	{
		Game.AssertServer();

		var player = pawn as ThinkerPlayer;
		Log.Info( player?.LastAttackerEntity != null
			? $"{client.Name} was killed by {player.LastAttackerEntity.Client?.Name}"
			: $"{client.Name} suicided" );

		if ( pawn.LastAttacker != null )
		{
			if ( pawn.LastAttacker.Client != null )
			{
				OnKilledMessage( pawn.LastAttacker.Client.SteamId, pawn.LastAttacker.Client.Name, client.SteamId, client.Name, pawn.LastAttackerWeapon?.ClassName, pawn.LastAttackerWeapon as Weapon );
			}
			else
			{
				OnKilledMessage( pawn.LastAttacker.NetworkIdent, pawn.LastAttacker.ToString(), client.SteamId, client.Name, "killed", null );
			}
		}
		else
		{
			OnKilledMessage( client.SteamId, client.Name, 0, "", "fucking died", null );
		}
	}

	[ClientRpc]
	public void OnKilledMessage( long leftid, string left, long rightid, string right, string method, Weapon weapon )
	{
		KillFeed.Current?.AddEntry( leftid, left, rightid, right, method, weapon );
	}
	
	[ConCmd.Admin("kill")]
	public static void Kill()
	{
		var player = (ThinkerPlayer)ConsoleSystem.Caller.Pawn;
		
		player.TakeDamage( DamageInfo.Generic( player.Health * 10 ) );
	}
	
	[ConCmd.Admin("hurt")]
	public static void Damage( int amount )
	{
		var player = (ThinkerPlayer)ConsoleSystem.Caller.Pawn;
		
		player.TakeDamage( DamageInfo.Generic( amount ) );
	}
}
