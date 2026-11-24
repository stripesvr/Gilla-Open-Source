// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class ShopRooms : MonoBehaviour
{
	public Bounds[] rooms = new Bounds[0];
	private static ShopRooms here;

	private void OnEnable()
	{

		here = this;
	}

	private void OnDisable()
	{
		if (here == this)
		{
			here = null;
		}
	}

	public static bool Holds(Vector3 at)
	{
		if (here != null)
		{
			Bounds[] r = here.rooms;
			for (int i = 0; i < r.Length; i++)
			{
				if (r[i].Contains(at))
				{
					return true;
				}
			}
		}
		return false;
	}
}
