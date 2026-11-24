// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class GillaMapVote : MonoBehaviour
{
    private void Draw(GillaModeRound round)
    {
        NetworkBootstrap bootstrap = NetworkBootstrap.Instance;
        PlayerRef me = round != null ? round.Runner.LocalPlayer : PlayerRef.None;

        byte myVote;
        if (round != null)
        {
            byte v;
            if (round.Votes.TryGet(me, out v))
                myVote = v;
            else
                myVote = 255;
        }
        else
        {
            myVote = 255;
        }

        string state;
        string rest;
        if (round == null)
        {
            state = "none";
            bool joining = bootstrap != null ? bootstrap.Joining : false;
            rest = joining.ToString();
        }
        else
        {
            state = string.Format("{0}|{1}|{2}|{3},{4},{5}|{6}",
                round.Phase, round.Map, myVote,
                round.VotesFor(0), round.VotesFor(1), round.VotesFor(2),
                (int)(round.VoteEnds - round.Now));
            rest = string.Format("|{0}|{1}|{2}|{3}|{4}|{5}{6}|{7}",
                (int)(round.HuntEnds - round.Now),
                (int)(round.OverEnds - round.Now),
                round.Apes(),
                round.Winner,
                round.Monster == me,
                inHunt,
                outOfIt,
                round.Enough);
        }
        state = state + rest;

        int chance;
        if (round != null && round.Enough)
            chance = Percent(round.MonsterChance(me));
        else
            chance = -1;

        int countForKey = bootstrap != null ? bootstrap.PlayerCount : 0;
        string key = string.Concat(new string[] { state, "|", countForKey.ToString(), "|", chance.ToString() });
        if (key == drawn)
            return;
        drawn = key;

        for (int i = 0; i < panelCanvas.Length && i < maps.Length; i++)
        {
            Canvas canvas = panelCanvas[i];
            if (canvas == null)
                continue;

            canvas.Clear(Black, Scan);

            bool mine = round != null && round.Phase == 0 && i == myVote;
            bool playing = round != null && round.Phase != 0 && i == round.Map;
            bool bright = (round == null || round.Phase == 0) ? true : playing;

            Color32 inner;
            if (mine)
            {
                canvas.Outline(new RectInt(0, 0, 256, 208), Picked);
                inner = Picked;
            }
            else if (playing)
            {
                canvas.Outline(new RectInt(0, 0, 256, 208), Lit);
                inner = Lit;
            }
            else
            {
                canvas.Outline(new RectInt(0, 0, 256, 208), Mid);
                inner = Low;
            }
            canvas.Outline(new RectInt(1, 1, 254, 206), inner);

            canvas.Text(title, 10, 6, maps[i].label, bright ? HeadText : DimText, 2, false);

            Renderer quad = i < panelCovers.Length ? panelCovers[i] : null;
            float b = bright ? 1f : 0.35f;
            if (!ShowCover(quad, maps[i].cover, b))
            {
                if (i < pictures.Length)
                {
                    Color32[] pic = pictures[i];
                    if (pic != null)
                    {
                        canvas.Picture(pic, maps[i].preview.width, maps[i].preview.height, 8, 30, 2, b);
                    }
                }
            }

            if (round != null)
            {
                if (round.Phase == 0)
                {
                    int votes = round.VotesFor(i);
                    string label = votes.ToString() + (votes == 1 ? " VOTE" : " VOTES");
                    int w = text == null ? 40 : text.Measure(label, 1);
                    canvas.Text(text, 246 - w, 10, label, votes < 1 ? DimText : WhiteText, 1, false);
                    for (int k = 0; k < votes; k++)
                    {
                        canvas.Fill(new RectInt(12 + 10 * k, 166, 7, 7), Lit);
                    }
                    canvas.Text(text, 128, 186, mine ? "YOUR VOTE" : "POKE TO VOTE", mine ? PickedText : WhiteText, 2, true);
                }
                else if (playing)
                {
                    canvas.Text(text, 128, 186, round.Phase == 1 ? "PLAYING NOW" : "GAME OVER", HeadText, 2, true);
                }
            }
            canvas.Show();
        }

        Canvas board = boardCanvas;
        if (board == null)
            return;

        board.Clear(Black, Scan);
        board.Outline(new RectInt(0, 0, 512, 128), Mid);
        for (int x = 8; x < 504; x += 14)
        {
            board.Fill(new RectInt(x, 2, 8, 2), Lit);
        }
        board.Text(title, 256, 8, "BE THE MONSTER", HeadText, 2, true);

        int players = bootstrap != null ? bootstrap.PlayerCount : 0;

        string head;
        string clock;
        string line;
        Color32[] clockColor;
        if (round == null)
        {
            clock = "-:--";
            head = "OFFLINE";
            if (bootstrap != null && bootstrap.Joining)
                head = "CONNECTING...";
            line = "THE VOTE NEEDS A ROOM ON THE NETWORK";
            clockColor = DimText;
        }
        else if (round.Phase == 0)
        {
            float voteEnds = round.VoteEnds;
            int secs;
            if (voteEnds <= 0f)
            {
                head = "VOTE FOR A MAP";
                if (!round.Enough)
                    head = string.Format("NEED {0} PLAYERS", GillaModeRound.LeastPlayers);
                secs = Mathf.RoundToInt(GillaModeRound.VoteSeconds) /* ? */;
            }
            else
            {
                head = "VOTING ENDS IN";
                secs = Mathf.Max((int)(round.VoteEnds - round.Now), 0);
            }
            clock = Clock(secs);
            clockColor = voteEnds <= 0f ? DimText : WhiteText;
            line = round.Enough ? "POKE A MAP TO VOTE. ONE OF YOU WILL BE THE MONSTER" : "WAITING FOR ANOTHER PLAYER. POKE A MAP TO VOTE";
        }
        else if (round.Phase != 1)
        {
            head = round.Winner == 1 ? "THE MONSTER WINS!" : "THE APES SURVIVED!";
            clock = Clock(Mathf.Max((int)(round.OverEnds - round.Now), 0));
            line = "BACK TO THE LOBBY WHEN THE CLOCK RUNS OUT";
            clockColor = PickedText;
        }
        else
        {
            var apes = round.Apes();
            head = "THE HUNT IS ON";
            clock = Clock(Mathf.Max((int)(round.HuntEnds - round.Now), 0));
            clockColor = HeadText;
            string fmt;
            if (round.Monster == me)
                fmt = "YOU ARE THE MONSTER. {0} OF {1} APES LEFT";
            else if (!inHunt || outOfIt)
                fmt = "{0} OF {1} APES LEFT";
            else
                fmt = "HIDE! {0} OF {1} APES LEFT";
            line = string.Format(fmt, apes.Item2, apes.Item1);
        }

        Color32[] headColor = (round != null && round.Phase == 2) ? PickedText : WhiteText;
        board.Text(text, 256, 28, head, headColor, 2, true);
        board.Text(text, 256, 46, clock, clockColor, 6, true);
        board.Text(text, 256, 96, line, HeadText, 1, true);

        string count = "PLAYERS IN THE ROOM: " + players.ToString();
        string gap = "     ";
        if (chance < 0 || text == null)
        {
            board.Text(text, 256, 110, count, DimText, 1, true);
            ShownLine = count;
            board.Show();
            return;
        }

        string chanceText = chance.ToString() + "% CHANCE TO BE THE MONSTER" + (round.Phase != 0 ? " NEXT ROUND" : "");
        int total = text.Measure(count + gap + chanceText, 1);
        int left = (512 - total) / 2;
        board.Text(text, left, 110, count, DimText, 1, false);
        int first = text.Measure(count + gap, 1);
        board.Text(text, first + left, 110, chanceText, HeadText, 1, false);
        ShownLine = count + gap + chanceText;
        board.Show();
    }

    private static string Clock(int seconds)
    {
        return (seconds / 60).ToString() + ":" + (seconds % 60).ToString("00");
    }

    private static int Percent(float share)
    {
        if (share <= 0f)
            return 0;
        if (1f <= share)
            return 100;
        int p = (int)Math.Round((double)(share * 100f));
        if (p > 98)
            p = 99;
        if (p < 2)
            p = 1;
        return p;
    }

    private static bool ShowCover(Renderer quad, Texture2D cover, float bright)
    {
        if (quad == null)
            return false;
        quad.enabled = cover != null;
        if (cover == null)
            return false;
        if (coverBlock == null)
            coverBlock = new MaterialPropertyBlock();
        quad.GetPropertyBlock(coverBlock);
        coverBlock.SetTexture(BaseMap, cover);
        coverBlock.SetColor(BaseColor, new Color(bright, bright, bright, 1f));
        quad.SetPropertyBlock(coverBlock);
        return true;
    }

    public GillaMapVote()
    {
        maps = new MapEntry[0];
        panels = new PressKey[0];
        panelScreens = new Renderer[0];
        panelCovers = new Renderer[0];
        monsterName = "Grinner";
        panelCanvas = new Canvas[0];
        pictures = new Color32[0][];
        seenPhase = 255;
        seenRound = -1;
        lastTick = -1;
    }

    static GillaMapVote()
    {
        BaseMap = Shader.PropertyToID("_BaseMap");
        Black = new Color32(6, 6, 6, 255);
        Scan = new Color32(13, 13, 13, 255);
        Lit = GillaBoardLook.Dots;
        Mid = new Color32(96, 96, 96, 255);
        Low = new Color32(40, 40, 40, 255);
        Picked = new Color32(255, 255, 255, 255);
        Grey = new Color32(90, 90, 90, 255);
        HeadText = GillaBoardLook.White;
        WhiteText = GillaBoardLook.Soft;
        PickedText = GillaBoardLook.White;
        DimText = GillaBoardLook.Dim;
        BaseColor = Shader.PropertyToID("_BaseColor");
    }
}
