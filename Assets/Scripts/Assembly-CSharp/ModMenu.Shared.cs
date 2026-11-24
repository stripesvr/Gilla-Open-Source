// Redux: lets other panels (the jukebox's music panel) use the mod menu's own look.
using UnityEngine;

public partial class ModMenu
{
    public static Sprite PanelBack(int w, int h) { return MakeSimpleBack(w, h, true); }
    public static Sprite RoundedSprite(int size, float radius, float feather, bool point) { return MakeRounded(size, radius, feather, point); }
    public static readonly Color RowOff = new Color(0.078431375f, 0.078431375f, 0.078431375f, 1f);
    public static readonly Color RowEdge = new Color(0.47058824f, 0.47058824f, 0.47058824f, 1f);
    public static readonly Color FaintText = new Color(1f, 1f, 1f, 0.55f);
}
