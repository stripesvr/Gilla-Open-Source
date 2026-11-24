// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class CosmeticSkin : MonoBehaviour
{
	[Tooltip("The body's look: replaces the ape's fur material on every piece of the body.")]
	public Material body;
	[Tooltip("Takes the wearer's fur colour (its _FurColor) where its texture's alpha says: the Dragon's scales.")]
	public bool furTint;
	[Tooltip("The eyes' look (none: the ape's own, or the eye cosmetic).")]
	public Material eyes;
	[Tooltip("What sprays out of a torn part instead of blood (none: blood).")]
	public Material blood;
	[Tooltip("Only the body pieces whose names have one of these in them (Torso, Arm, Head); none: every piece.")]
	public string[] pieces = new string[0];
	[Tooltip("A suit worn over the body, as Lethal Ape Redux's VIP suit: a mesh over each body piece it covers (made from that piece: the same bones), drawn on it in Suit Material. The fur under it stays as it is.")]
	public Mesh[] suitMeshes = new Mesh[0];
	[Tooltip("Which body piece each suit mesh goes over (its renderer's name: Gilla_Torso, Gilla_ArmL...).")]
	public string[] suitPieces = new string[0];
	public Material suitMaterial;
	[Tooltip("Worn with the suit over the torso piece, bent with it on its bones (its weights the suit's own where it lies): the VIP's jacket front, its first part in Suit Material (the lapels), its second in Suit Cloth (the shirt, its collar, the buttons, the pocket square). Not on your first-person body (your eyes are inside the suit's neck), nor on a body left behind.")]
	public Mesh suitTrim;
	public Material suitCloth;
	public static readonly int FurColor = Shader.PropertyToID("_FurColor");

	public bool Covers(Renderer r)
	{
		if (pieces == null || pieces.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < pieces.Length; i++)
		{
			string value = pieces[i];
			if (!string.IsNullOrEmpty(value))
			{
				if (r.name.Contains(value))
				{
					return true;
				}
			}
		}
		return false;
	}
}
