<p align="center">
  <img src=".github/banner.png" alt="Gilla Redux" width="100%">
</p>

A fan-made, open-source rebuild of Gilla, the VR monkey game for Meta Quest. Built in Unity 6 with Photon Fusion.

> Unofficial fan project. Not affiliated with or endorsed by the original Gilla developers.

## The game

You play as an ape in a shared world: climb, grab, throw and fight, and team up with or turn on other players.

- Monsters that hunt, grab and tear players apart, and you can play as one
- Game modes, including Be The Monster and Don't Splash
- Gold, bolts and torn-off body parts you can grab and throw
- Doors that crush your arms off
- A shop with colours, cosmetics and a size menu
- An in-game computer
- A jukebox you can carry around that plays songs for the whole room

## Getting started

1. Install Unity **6000.3.27f1**, with Android Build Support if you want to build for Quest.
2. Clone this repo and add the folder in Unity Hub. The first import takes a while.
3. Open `Assets/Scenes/Loading.unity` and press Play.

## Online setup

The private keys and backend aren't included. To play online, add your own free IDs:

| Setting | File |
|---------|------|
| Photon Fusion app ID | `Assets/Resources/PhotonAppSettings.asset` (`AppIdFusion`) |
| Photon Voice app ID | `Assets/Resources/PhotonAppSettings.asset` (`AppIdVoice`) |
| PlayFab title ID | `Assets/Resources/PlayFabSharedSettings.asset` (`TitleId`) |

## Notes

This is a hobby project, so it's not perfect. Some textures and shaders are approximations of the original, and some systems are still rough. Bug reports, fixes and ideas are welcome.
