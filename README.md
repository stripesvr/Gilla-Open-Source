# Gilla Redux

A small fan-made rebuild of **Gilla**, a VR monkey game for Meta Quest, made in Unity 6 with Photon Fusion. It started as a learning project and grew from there.

> Unofficial fan project. Not affiliated with or endorsed by the original Gilla developers.

## About the game

You play as an ape in a shared world. You can climb, grab, throw and fight, and team up with or turn on other players.

- **Monsters** that hunt, grab and tear players apart, and you can play as one yourself
- **Game modes**, including *Be The Monster* and *Don't Splash*
- **Physical interactions:** pick up gold, bolts and loose body parts; doors can crush your arms off
- **A shop and cosmetics** with player colours and a size menu
- **An in-game computer** for settings and music
- **A jukebox** you can carry around that plays songs you search for to everyone in the room

## Requirements

- Unity **6000.3.27f1** (URP)
- Android build support, for Quest builds
- A Meta Quest headset, or play in the editor with mouse and keyboard

## Getting started

1. Clone or download this repository.
2. In Unity Hub, choose **Add** and pick this folder. The first import takes a while.
3. Open `Assets/Scenes/Loading.unity` and press **Play**.

## Online setup

The private keys and backend aren't included, so online play, saving and accounts won't connect until you add your own:

| What | Where |
|------|-------|
| Photon Fusion and Voice app IDs | `Assets/Resources/PhotonAppSettings.asset` (`AppIdFusion`, `AppIdVoice`) |
| PlayFab title ID | `Assets/Resources/PlayFabSharedSettings.asset` (`TitleId`) |

You can get free IDs from the [Photon dashboard](https://dashboard.photonengine.com) and [PlayFab](https://playfab.com).

## Known issues

This is a hobby project and it's far from perfect:

- Some textures and shaders are approximations of the original look.
- Some systems are still rough, and parts of the code were rebuilt by hand and could be cleaner.
- Meta usernames need your own Meta App ID.

## Contributing

Feedback, bug reports, fixes and ideas are all welcome. Open an issue or a pull request.

Thanks for checking it out!
