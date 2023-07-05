using System.Collections.Generic;
using Sandbox;

namespace ThekiFake.TheThinker;

public partial class AmmoContainer : BaseNetworkable
{
	public static readonly IList<int> DefaultAmmo = new List<int>
	{
		60
	};

	public static int AmmoTypes => DefaultAmmo.Count;
	[Net, Predicted] public IList<int> Ammo { get; set; }

	public AmmoContainer()
	{
		Ammo = DefaultAmmo;
	}

	public int Get( AmmoType type )
	{
		return Ammo[(int)type];
	}

	public void Set( AmmoType type, int value )
	{
		Ammo[(int)type] = value;
	}

	public void AddTo( IList<int> other )
	{
		Log.Info( Ammo[0] );
		for ( int i = 0; i < other.Count; i++ )
		{
			Ammo[i] += other[i];
		}
		Log.Info( Ammo[0] );
	}
}
