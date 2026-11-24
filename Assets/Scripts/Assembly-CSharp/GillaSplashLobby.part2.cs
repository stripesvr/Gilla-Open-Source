using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class GillaSplashLobby : MonoBehaviour
{
    private void Draw(GillaSplashGame game)
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        PlayerRef me = game != null ? game.Runner.LocalPlayer : PlayerRef.None;

        byte myVote;
        if (game != null)
        {
            byte v;
            myVote = game.Votes.TryGet(me, out v) ? v : (byte)255;
        }
        else
        {
            myVote = 255;
        }

        byte myState;
        if (game != null)
        {
            byte st;
            myState = game.Players.TryGet(me, out st) ? st : (byte)0;
        }
        else
        {
            myState = 0;
        }

        int racing = game != null ? game.Count(1) : 0;
        int throughCount = game != null ? game.Count(2) : 0;
        int outCount = game != null ? game.Count(3) : 0;

        string state;
        if (game == null)
        {
            bool joining = boot != null ? boot.Joining : false;
            state = "none" + joining.ToString();
        }
        else
        {
            state = string.Format("{0}|{1}|{2}|{3},{4},{5}|{6}", game.Phase, game.Map, myVote,
                game.VotesFor(0), game.VotesFor(1), game.VotesFor(2),
                Mathf.CeilToInt(game.VoteEnds - game.Now));
            state = state + string.Format("|{0}|{1}|{2}|{3},{4},{5}|{6}|{7}{8}",
                Mathf.CeilToInt(game.PhaseEnds - game.Now), game.Round, game.Tied,
                racing, throughCount, outCount, myState, inGame, through);
        }
        int count = boot != null ? boot.PlayerCount : 0;
        state = state + "|" + count.ToString();
        if (state == drawn)
        {
            return;
        }
        drawn = state;

        for (int i = 0; i < panelCanvas.Length && i < maps.Length; i++)
        {
            GillaMapVote.Canvas c = panelCanvas[i];
            if (c == null)
            {
                continue;
            }
            c.Clear(Black, Scan);
            bool voting = game != null ? game.Phase == 0 : false;
            bool mine = false;
            if (i == myVote)
            {
                mine = voting;
            }
            bool playing;
            if (voting || game == null)
            {
                playing = false;
            }
            else
            {
                playing = i == game.Map;
            }
            bool hasGame = game != null;
            if (mine)
            {
                c.Outline(new RectInt(0, 0, 256, 208), Picked);
                c.Outline(new RectInt(1, 1, 254, 206), Picked);
            }
            else if (playing)
            {
                c.Outline(new RectInt(0, 0, 256, 208), Lit);
                c.Outline(new RectInt(1, 1, 254, 206), Lit);
            }
            else
            {
                c.Outline(new RectInt(0, 0, 256, 208), Mid);
                c.Outline(new RectInt(1, 1, 254, 206), Low);
            }
            bool dimmed = hasGame && (!voting && !playing);
            c.Text(title, 10, 6, maps[i].label, dimmed ? DimText : HeadText, 2, false);

            Renderer quad = i < panelCovers.Length ? panelCovers[i] : null;
            float alpha = 1f;
            if (dimmed)
            {
                alpha = 0.35f;
            }
            // GillaMapVote.ShowCover is not accessible here: same intent inline.
            bool coverShown = false; /* ? */
            Texture2D cover = maps[i].cover;
            if (quad != null)
            {
                if (cover != null)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    quad.GetPropertyBlock(block);
                    block.SetTexture("_MainTex", cover);
                    block.SetTexture("_BaseMap", cover);
                    Color tint = new Color(1f, 1f, 1f, alpha);
                    block.SetColor("_Color", tint);
                    block.SetColor("_BaseColor", tint);
                    quad.SetPropertyBlock(block);
                    quad.enabled = true;
                    coverShown = true;
                }
                else
                {
                    quad.enabled = false;
                }
            }
            if (!coverShown)
            {
                if (i < pictures.Length)
                {
                    Color32[] pic = pictures[i];
                    if (pic != null)
                    {
                        int w = maps[i].preview.width;
                        int h = maps[i].preview.height;
                        c.Picture(pic, w, h, 8, 30, 2, alpha);
                    }
                }
            }

            if (voting)
            {
                int votes = game.VotesFor(i);
                string label = votes.ToString() + (votes != 1 ? " VOTES" : " VOTE");
                int width = text == null ? 40 : text.Measure(label, 1);
                c.Text(text, 246 - width, 10, label, votes < 1 ? DimText : WhiteText, 1, false);
                for (int k = 0; k < votes; k++)
                {
                    c.Fill(new RectInt(12 + k * 10, 166, 7, 7), Lit);
                }
                c.Text(text, 128, 186, mine ? "YOUR VOTE" : "POKE TO VOTE", mine ? PickedText : WhiteText, 2, true);
            }
            else if (playing)
            {
                c.Text(text, 128, 186, game.Phase != 4 ? "PLAYING NOW" : "GAME OVER", HeadText, 2, true);
            }
            c.Show();
        }

        Color32[] whatColours = WhiteText;
        int players = boot != null ? boot.PlayerCount : 0;
        string what;
        string clock;
        Color32[] clockColours;
        string small;
        if (game == null)
        {
            string s1 = "OFFLINE";
            if (boot != null && boot.Joining)
            {
                s1 = "CONNECTING...";
            }
            what = s1;
            clock = "-:--";
            small = "THE GAME NEEDS A ROOM ON THE NETWORK";
            clockColours = DimText;
        }
        else if (game.Phase == 0)
        {
            float ends = game.VoteEnds;
            what = 0f < ends ? "VOTING ENDS IN" : "VOTE FOR A MAP";
            int secs;
            if (0f < ends)
            {
                secs = Mathf.Max(Mathf.CeilToInt(game.VoteEnds - game.Now), 0);
            }
            else
            {
                secs = Mathf.CeilToInt(GillaSplashGame.VoteSeconds);
            }
            clock = Clock(secs);
            clockColours = 0f < ends ? WhiteText : DimText;
            small = "POKE A MAP. GET TO THE END BEFORE THE TIME'S UP";
        }
        else if (game.Phase == 1)
        {
            what = string.Format("ROUND {0} OF {1}: GET READY", game.Round, 7);
            clock = Clock(Mathf.Max(Mathf.CeilToInt(game.PhaseEnds - game.Now), 0));
            clockColours = PickedText;
            string race = Clock(Mathf.CeilToInt(GillaSplashGame.RaceSeconds(game.Round)));
            string extra;
            if (game.Dark)
            {
                extra = ". LIGHTS OUT!";
            }
            else
            {
                extra = game.Sinking ? ". PLATFORMS SINK!" : "";
            }
            small = "GET TO THE END IN " + race + extra;
        }
        else if (game.Phase == 2)
        {
            what = string.Format("ROUND {0} OF {1}", game.Round, 7);
            string extra;
            if (game.Dark)
            {
                extra = ": LIGHTS OUT!";
            }
            else
            {
                extra = game.Sinking ? ": THEY SINK!" : "";
            }
            what = what + extra;
            clock = Clock(Mathf.Max(Mathf.CeilToInt(game.PhaseEnds - game.Now), 0));
            clockColours = HeadText;
            small = string.Format("{0} THROUGH, {1} TO GO. DON'T SPLASH!", throughCount, racing);
        }
        else if (game.Phase == 3)
        {
            what = string.Format("ROUND {0} OVER", game.Round);
            clock = Clock(Mathf.Max(Mathf.CeilToInt(game.PhaseEnds - game.Now), 0));
            clockColours = WhiteText;
            small = "NO ONE MADE IT: GAME OVER";
            if (0 < throughCount)
            {
                small = string.Format("{0} THROUGH. THE REST ARE OUT", throughCount);
            }
        }
        else
        {
            bool tied = game.Tied;
            what = (tied ? "A TIE: " : "WINNER: ") + Winners(game);
            whatColours = PickedText;
            clock = Clock(Mathf.Max(Mathf.CeilToInt(game.PhaseEnds - game.Now), 0));
            clockColours = PickedText;
            small = "BACK TO THE LOBBY WHEN THE CLOCK RUNS OUT";
        }

        string foot;
        if (game != null && myState == 1 && game.Phase != 4)
        {
            foot = "YOU'RE IN. GET TO THE END!";
        }
        else if (game != null && myState == 2)
        {
            foot = "YOU'RE THROUGH!";
        }
        else if (game != null && myState == 3)
        {
            foot = "YOU'RE OUT. THE NEXT GAME STARTS FROM THE VOTE";
        }
        else if (game != null && myState == 4)
        {
            foot = "IT GOT YOU. WATCH THE REST ON THE SCREEN";
        }
        else
        {
            foot = "PLAYERS IN THE ROOM: " + players.ToString();
        }

        Board(boardCanvas, "DON'T SPLASH", what, whatColours, clock, clockColours, small, foot);

        int level = game != null ? game.Level : 0;
        for (int i = 0; i < roomCanvas.Length; i++)
        {
            GillaMapVote.Canvas c = roomCanvas[i];
            if (c == null)
            {
                continue;
            }
            if (game != null && level == i && game.Phase == 1)
            {
                int secs = Mathf.Max(Mathf.CeilToInt(game.PhaseEnds - game.Now), 0);
                string head = string.Format("ROUND {0}", game.Round);
                Board(c, head, 3 < secs ? "THE GATE OPENS IN" : "GET READY...", WhiteText, secs.ToString(), PickedText, small, foot);
                continue;
            }
            if (game != null && level == i && game.Phase == 2)
            {
                float left = game.PhaseEnds - game.Now;
                if (GillaSplashGame.RaceSeconds(game.Round) - 2.5f < left)
                {
                    string head = string.Format("ROUND {0}", game.Round);
                    Board(c, head, "GO GO GO!", PickedText, "GO!", PickedText, small, foot);
                    continue;
                }
            }
            if (game != null && level + 1 == i && game.Phase == 2)
            {
                string line;
                Color32[] lineColours;
                if (myState == 2)
                {
                    line = "YOU'RE THROUGH!";
                    lineColours = PickedText;
                }
                else
                {
                    line = "MADE IT? STAY HERE";
                    lineColours = WhiteText;
                }
                string roomClock = Clock(Mathf.Max(Mathf.CeilToInt(game.PhaseEnds - game.Now), 0));
                string info = string.Format("{0} THROUGH SO FAR, {1} STILL RACING", throughCount, racing);
                Board(c, "THE FINISH", line, lineColours, roomClock, HeadText, info, "THE REST HAVE TILL THE CLOCK RUNS OUT");
                continue;
            }
            string roomHead = level + 1 != i ? "DON'T SPLASH" : "THE FINISH";
            if (level == i)
            {
                roomHead = "THE START";
            }
            Board(c, roomHead, what, whatColours, clock, clockColours, small, foot);
        }
    }

    private void Board(GillaMapVote.Canvas b, string head, string what, Color32[] whatColours, string clock, Color32[] clockColours, string small, string foot)
    {
        if (b == null)
        {
            return;
        }
        b.Clear(Black, Scan);
        b.Outline(new RectInt(0, 0, 512, 128), Mid);
        for (int x = 8; x < 504; x += 14)
        {
            b.Fill(new RectInt(x, 2, 8, 2), Lit);
        }
        b.Text(title, 256, 8, head, HeadText, 2, true);
        b.Text(text, 256, 28, what, whatColours, 2, true);
        b.Text(text, 256, 46, clock, clockColours, 6, true);
        b.Text(text, 256, 96, small, HeadText, 1, true);
        Color32[] footColours;
        if (foot.StartsWith("YOU'RE OUT") || foot.StartsWith("IT GOT"))
        {
            footColours = WhiteText;
        }
        else if (foot.StartsWith("YOU"))
        {
            footColours = PickedText;
        }
        else
        {
            footColours = DimText;
        }
        b.Text(text, 256, 110, foot, footColours, 1, true);
        b.Show();
    }

    private static string Clock(int seconds)
    {
        return (seconds / 60).ToString() + ":" + (seconds % 60).ToString("00");
    }

    public GillaSplashLobby()
    {
        maps = new MapEntry[0];
        panels = new PressKey[0];
        panelScreens = new Renderer[0];
        panelCovers = new Renderer[0];
        roomBoards = new Renderer[0];
        starts = new Transform[0];
        finishes = new Bounds[0];
        gates = new GillaSplashGate[0];
        roomCanvas = new GillaMapVote.Canvas[0];
        panelCanvas = new GillaMapVote.Canvas[0];
        pictures = new Color32[0][];
        seenPhase = 255;
        seenStarts = -1;
        lastTick = -1;
    }

    static GillaSplashLobby()
    {
        Black = new Color32(6, 6, 6, 255);
        Scan = new Color32(13, 13, 13, 255);
        Lit = GillaBoardLook.Dots;
        Mid = new Color32(96, 96, 96, 255);
        Low = new Color32(40, 40, 40, 255);
        Picked = new Color32(255, 255, 255, 255);
        HeadText = GillaBoardLook.White;
        WhiteText = GillaBoardLook.Soft;
        PickedText = GillaBoardLook.White;
        DimText = GillaBoardLook.Dim;
    }
}
