// Redux addition: the MUSIC page. Type a song, ENTER searches, 1-5 plays a result on the shop
// boombox for the whole room. OPTION 1 types a space, OPTION 3 stops the song.
using UnityEngine;

public partial class GillaComputer
{
    private static string musicTyped = "";
    private static bool musicPicking;

    private static void MusicKey(string id, bool digit)
    {
        if (id == "BACK")
        {
            if (musicTyped.Length > 0) musicTyped = musicTyped.Substring(0, musicTyped.Length - 1);
            musicPicking = false;
            return;
        }
        if (id == "ENTER")
        {
            if (musicTyped.Trim().Length > 0)
            {
                GillaMusic.Search(musicTyped);
                musicPicking = true;
            }
            return;
        }
        if (id == "OPTION 1")
        {
            if (musicTyped.Length < 30 && musicTyped.Length > 0 && !musicTyped.EndsWith(" ")) musicTyped += " ";
            musicPicking = false;
            return;
        }
        if (id == "OPTION 3")
        {
            GillaMusic.StopForAll();
            return;
        }
        if (digit && musicPicking && GillaMusic.Results.Count > 0)
        {
            int n = id[0] - '1';
            if (n >= 0 && n < GillaMusic.Results.Count) GillaMusic.Pick(n);
            return;
        }
        if (id.Length == 1 && char.IsLetterOrDigit(id[0]) && musicTyped.Length < 30)
        {
            musicTyped += id;
            musicPicking = false;
        }
    }

    private void DrawMusic(ScreenFont.Style hint)
    {
        ScreenFont.Style head = new ScreenFont.Style { scale = 2, slant = 0f, colours = Heading, along = true };
        ScreenFont.Style sel = new ScreenFont.Style { bold = true, scale = 1, slant = 0f, colours = Milk };
        ScreenFont.Style dim = new ScreenFont.Style { bold = false, scale = 1, slant = 0f, colours = Dim };
        Line(text, 198, 40, "MUSIC", head, true);
        Line(text, 86, 58, "SEARCH: " + musicTyped + "_", sel, false);
        int y = 72;
        if (musicPicking)
        {
            if (GillaMusic.Searching || GillaMusic.Results.Count == 0)
            {
                Line(text, 96, y, GillaMusic.Status, dim, false);
            }
            else
            {
                for (int i = 0; i < GillaMusic.Results.Count && i < 5; i++)
                {
                    GillaMusic.Result r = GillaMusic.Results[i];
                    string label = r.Label;
                    if (label.Length > 30) label = label.Substring(0, 30);
                    Line(text, 86, y, (i + 1) + " " + label, sel, false);
                    y += 11;
                }
            }
        }
        string now = GillaMusic.NowPlaying;
        if (now.Length > 0)
        {
            if (now.Length > 34) now = now.Substring(0, 34);
            Line(text, 86, 132, "NOW: " + now, sel, false);
        }
        Line(text, 160, 172, "ENTER search, 1-5 play it", hint, true);
        Line(text, 160, 181, "OPTION 1 space, OPTION 3 stop", hint, true);
        Line(text, 160, 190, "Plays on the shop boombox", hint, true);
    }
}
