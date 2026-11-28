"""Draws the Stars / Forks / Views tiles shown under the README banner.

Run by .github/workflows/stat-tiles.yml, which publishes the output folder
to the `badges` branch. Standard library only.

    python stat_tiles.py <out_dir>

<out_dir>/state.json is read if present (carried over from the last run)
and rewritten along with stars.svg, forks.svg and views.svg.
"""
import json
import os
import re
import sys
import urllib.request

REPO = os.environ.get("GITHUB_REPOSITORY", "stripesvr/Gilla-Open-Source")
TOKEN = os.environ.get("GITHUB_TOKEN", "")
HITS_URL = f"https://hits.sh/github.com/{REPO}.svg"

ICONS = {
    "stars": "M8 .25a.75.75 0 0 1 .673.418l1.882 3.815 4.21.612a.75.75 0 0 1 .416 1.279l-3.046 2.97.719 4.192a.751.751 0 0 1-1.088.791L8 12.347l-3.766 1.98a.75.75 0 0 1-1.088-.79l.72-4.194L.818 6.374a.75.75 0 0 1 .416-1.28l4.21-.611L7.327.668A.75.75 0 0 1 8 .25Z",
    "forks": "M5 5.372v.878c0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75v-.878a2.25 2.25 0 1 1 1.5 0v.878a2.25 2.25 0 0 1-2.25 2.25h-1.5v2.128a2.251 2.251 0 1 1-1.5 0V8.5h-1.5A2.25 2.25 0 0 1 3.5 6.25v-.878a2.25 2.25 0 1 1 1.5 0ZM5 3.25a.75.75 0 1 0-1.5 0 .75.75 0 0 0 1.5 0Zm6.75.75a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Zm-3 8.75a.75.75 0 1 0-1.5 0 .75.75 0 0 0 1.5 0Z",
    "views": "M8 2c1.981 0 3.671.992 4.933 2.078 1.27 1.091 2.187 2.345 2.637 3.023a1.62 1.62 0 0 1 0 1.798c-.45.678-1.367 1.932-2.637 3.023C11.67 13.008 9.981 14 8 14c-1.981 0-3.671-.992-4.933-2.078C1.797 10.83.88 9.576.43 8.898a1.62 1.62 0 0 1 0-1.798c.45-.677 1.367-1.931 2.637-3.022C4.33 2.992 6.019 2 8 2ZM1.679 7.932a.12.12 0 0 0 0 .136c.411.622 1.241 1.75 2.366 2.717C5.176 11.758 6.527 12.5 8 12.5c1.473 0 2.825-.742 3.955-1.715 1.124-.967 1.954-2.096 2.366-2.717a.12.12 0 0 0 0-.136c-.412-.621-1.242-1.75-2.366-2.717C10.824 4.242 9.473 3.5 8 3.5c-1.473 0-2.825.742-3.955 1.715-1.124.967-1.954 2.096-2.366 2.717ZM10 8a2 2 0 1 1-4.001-.001A2 2 0 0 1 10 8Z",
}

FONT = "Bahnschrift, 'DIN Alternate', 'Segoe UI', 'Helvetica Neue', Arial, sans-serif"
H = 68


def fetch(url, headers=None):
    req = urllib.request.Request(url, headers={"User-Agent": "stat-tiles", **(headers or {})})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.read().decode("utf-8", "replace")


def pretty(n):
    if n < 10000:
        return f"{n:,}"
    if n < 1000000:
        return f"{n / 1000:.1f}".rstrip("0").rstrip(".") + "k"
    return f"{n / 1000000:.1f}".rstrip("0").rstrip(".") + "M"


def tile(kind, value, width):
    label = kind.upper()
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{H}" viewBox="0 0 {width} {H}" role="img" aria-label="{value} {kind}">
<title>{value} {kind}</title>
<defs>
<linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1c1411"/><stop offset="1" stop-color="#0b0909"/></linearGradient>
<radialGradient id="glow" cx="50%" cy="118%" r="85%"><stop offset="0" stop-color="#e06a30" stop-opacity=".62"/><stop offset=".55" stop-color="#8a3a18" stop-opacity=".22"/><stop offset="1" stop-color="#8a3a18" stop-opacity="0"/></radialGradient>
<linearGradient id="or" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#f9a57e"/><stop offset="1" stop-color="#cf6239"/></linearGradient>
<linearGradient id="wh" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fbf9f6"/><stop offset="1" stop-color="#cfccca"/></linearGradient>
<linearGradient id="bd" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffa878" stop-opacity=".62"/><stop offset="1" stop-color="#ffa878" stop-opacity=".16"/></linearGradient>
<pattern id="dots" width="11" height="11" patternUnits="userSpaceOnUse"><circle cx="5.5" cy="5.5" r=".75" fill="#ffaa78" fill-opacity=".17"/></pattern>
<filter id="blur" x="-80%" y="-80%" width="260%" height="260%"><feGaussianBlur stdDeviation="5"/></filter>
<clipPath id="card"><rect width="{width}" height="{H}" rx="15"/></clipPath>
</defs>
<g clip-path="url(#card)">
<rect width="{width}" height="{H}" fill="url(#bg)"/>
<rect width="{width}" height="{H}" fill="url(#dots)"/>
<rect width="{width}" height="{H}" fill="url(#glow)"/>
</g>
<rect x=".75" y=".75" width="{width - 1.5}" height="{H - 1.5}" rx="14.25" fill="none" stroke="url(#bd)" stroke-width="1.5"/>
<g transform="translate(20 20) scale(1.75)">
<path d="{ICONS[kind]}" fill="#ff7a3c" fill-opacity=".75" filter="url(#blur)"/>
<path d="{ICONS[kind]}" fill="url(#or)"/>
</g>
<text x="64" y="36" font-family="{FONT}" font-size="27" font-weight="700" fill="url(#wh)">{value}</text>
<text x="65" y="53" font-family="{FONT}" font-size="11" font-weight="700" letter-spacing="2.4" fill="#e9906a">{label}</text>
</svg>
'''


def main(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    state_path = os.path.join(out_dir, "state.json")
    state = {"self_reads": 0, "views": 0, "stars": 0, "forks": 0}
    if os.path.exists(state_path):
        with open(state_path, encoding="utf-8") as f:
            state.update(json.load(f))

    # stars and forks straight from GitHub
    try:
        headers = {"Accept": "application/vnd.github+json"}
        if TOKEN:
            headers["Authorization"] = f"Bearer {TOKEN}"
        info = json.loads(fetch(f"https://api.github.com/repos/{REPO}", headers))
        state["stars"] = int(info["stargazers_count"])
        state["forks"] = int(info["forks_count"])
    except Exception as e:  # keep the last known numbers
        print("GitHub API failed:", e)

    # Views come from the hits.sh counter that the README loads as a 1px image.
    # Reading the counter here also bumps it, so those reads are subtracted.
    try:
        m = re.search(r'aria-label="[^"]*?(\d[\d,]*)"', fetch(HITS_URL))
        hits = int(m.group(1).replace(",", ""))
        state["self_reads"] += 1
        state["views"] = max(state["views"], hits - state["self_reads"])
    except Exception as e:
        print("View counter failed:", e)

    values = {k: pretty(state[k]) for k in ("stars", "forks", "views")}
    longest = max(len(v) for v in values.values())
    width = max(156, 64 + int(longest * 16.5) + 26)
    for kind, value in values.items():
        with open(os.path.join(out_dir, f"{kind}.svg"), "w", encoding="utf-8", newline="\n") as f:
            f.write(tile(kind, value, width))
    with open(state_path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(state, f, indent=2)
        f.write("\n")
    print(values, "width", width)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "tiles")
