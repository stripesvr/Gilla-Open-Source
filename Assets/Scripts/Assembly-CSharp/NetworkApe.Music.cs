// Redux addition: room-wide messages for the boombox (song picked on the computer, boombox carried).
using Fusion;
using UnityEngine;

public partial class NetworkApe
{
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Song(string url, string label)
    {
        GillaMusic.Play(url, label);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MusicLoop(NetworkBool on)
    {
        GillaMusic.SetLoop(on);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BoomboxAt(Vector3 pos, Quaternion rot)
    {
        if (Object != null && Object.HasStateAuthority) return;   // the sender moved it already
        GillaBoomboxGrab grab = GillaBoomboxGrab.Instance;
        if (grab != null) grab.Remote(pos, rot);
    }
}
