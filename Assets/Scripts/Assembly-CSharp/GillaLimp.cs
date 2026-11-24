// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[DefaultExecutionOrder(150)]
public class GillaLimp : MonoBehaviour
{
    private const float EaseBack = 0.35f;
    private const float NoWordAfter = 1.5f;
    private NetworkApe who;
    private ApeAvatarDriver driver;
    private GillaCorpse limp;
    private Transform[] real;
    private Transform[] copied;
    private Vector3[] startedAt;
    private Vector3[] posedAt;
    private Vector3[] drawnAt;
    private Vector3[] easedAt;
    private Quaternion[] startedTurned;
    private Quaternion[] posedTurned;
    private Quaternion[] drawnTurned;
    private Quaternion[] easedTurned;
    private bool heard;
    private bool pushed;
    private Vector3 way;
    private float since;
    private float easing;

    public bool IsDown
    {
        get { return limp != null; }
    }

    public Vector3 Eyes
    {
        get
        {
            if (limp != null)
            {
                return limp.HeadNow;
            }
            return transform.position;
        }
    }

    public Vector3 Hand(bool right)
    {
        if (limp != null)
        {
            return limp.HandNow(right);
        }
        return transform.position;
    }

    public Vector3 Middle
    {
        get
        {
            if (limp != null)
            {
                return limp.MiddleNow;
            }
            return transform.position;
        }
    }

    public Vector3 Front
    {
        get
        {
            if (limp != null)
            {
                return limp.FrontNow;
            }
            return Vector3.forward;
        }
    }

    public Vector3 Going
    {
        get
        {
            if (limp != null)
            {
                return limp.GoingNow;
            }
            return Vector3.zero;
        }
    }

    public Vector3 Way
    {
        get { return way; }
    }

    private void Awake()
    {

        who = GetComponent<NetworkApe>();
    }

    private void OnDestroy()
    {
        if (limp != null)
        {
            Destroy(limp.gameObject);
        }
    }

    public void Begin(Vector3 going)
    {
        Vector3 flat = Vector3.ProjectOnPlane(going, Vector3.up);
        if (limp != null)
        {
            if (pushed || flat.sqrMagnitude < 0.01f)
            {
                limp.Shove(going * 0.5f);
                limp.FallsFlat(Vector3.zero);
            }
            else
            {
                way = flat.normalized;
                limp.Shove(going);
                limp.FallsFlat(way);
                pushed = true;
            }
            since = Time.time;
            return;
        }

        if (who == null) return;
        if (who.tearing == null) return;
        if (who.ape == null) return;
        if (!who.ape.activeInHierarchy) return;

        if (0f < easing)
        {
            PutBackPosed();
            easing = 0f;
        }

        pushed = flat.sqrMagnitude >= 0.01f;
        if (!pushed)
        {
            flat = Vector3.ProjectOnPlane(who.head.forward, Vector3.up);
        }
        flat.Normalize();
        way = flat;

        limp = GillaCorpse.Limp(who.tearing, who.Owner.PlayerId /* ? */, going, way);

        var skeleton = limp.Skeleton /* ? */;
        int count = skeleton.Count;
        real = new Transform[count];
        copied = new Transform[count];
        startedAt = new Vector3[count];
        posedAt = new Vector3[count];
        drawnAt = new Vector3[count];
        easedAt = new Vector3[count];
        startedTurned = new Quaternion[count];
        posedTurned = new Quaternion[count];
        drawnTurned = new Quaternion[count];
        easedTurned = new Quaternion[count];

        int i = 0;
        foreach (var pair in skeleton)
        {
            real[i] = pair.Key;
            copied[i] = pair.Value;
            startedAt[i] = pair.Key.localPosition;
            startedTurned[i] = pair.Key.localRotation;
            i++;
        }

        if (driver == null)
        {
            driver = who.ape.GetComponentInChildren<ApeAvatarDriver>(true);
        }
        if (driver != null)
        {
            driver.enabled = false;
        }
        heard = false;
        since = Time.time;
    }

    public void End(bool ease)
    {
        if (limp == null)
        {
            return;
        }
        for (int i = 0; i < real.Length; i++)
        {
            drawnAt[i] = real[i].position;
            drawnTurned[i] = real[i].rotation;
        }
        for (int i = 0; i < real.Length; i++)
        {
            real[i].SetLocalPositionAndRotation(startedAt[i], startedTurned[i]);
        }
        if (driver != null)
        {
            driver.enabled = true;
        }
        Destroy(limp.gameObject);
        limp = null;
        easing = ease ? 0.35f : 0f;
    }

    private void Update()
    {
        if (easing <= 0f)
        {
            return;
        }
        for (int i = 0; i < real.Length; i++)
        {
            real[i].SetLocalPositionAndRotation(posedAt[i], posedTurned[i]);
        }
    }

    private void LateUpdate()
    {
        if (who == null)
        {
            return;
        }
        bool down = who.DownSaid;
        bool held = who.Drawn /* ? */ != null ? who.Drawn /* ? */.InHand : false;

        if (limp == null)
        {
            if (!held && down)
            {
                if (!who.IsMonster)
                {
                    Begin(Vector3.zero);
                }
            }
        }
        else
        {
            if (down)
            {
                heard = true;
            }
            if (!held)
            {
                if (who.IsMonster)
                {
                    End(true);
                }
                else if (!down)
                {
                    if (!heard)
                    {
                        if (1.5f < Time.time - since)
                        {
                            End(true);
                        }
                    }
                    else if (!who.Busy)
                    {
                        End(true);
                    }
                }
            }
            else
            {
                End(true);
            }
        }

        if (limp != null)
        {
            for (int i = 0; i < real.Length; i++)
            {
                Transform bone = real[i];
                Vector3 pos = copied[i].position;
                Quaternion rot = copied[i].rotation;
                bone.SetPositionAndRotation(pos, rot);
            }
            return;
        }

        float left = easing;
        if (0f < left)
        {
            float t = Mathf.Clamp01(1f - left / 0.35f);
            easing = left - Time.deltaTime;
            if (0f < easing)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                for (int i = 0; i < real.Length; i++)
                {
                    posedAt[i] = real[i].localPosition;
                    posedTurned[i] = real[i].localRotation;
                }
                for (int i = 0; i < real.Length; i++)
                {
                    easedAt[i] = Vector3.Lerp(drawnAt[i], real[i].position, s);
                    easedTurned[i] = Quaternion.Slerp(drawnTurned[i], real[i].rotation, s);
                }
                for (int i = 0; i < real.Length; i++)
                {
                    real[i].SetPositionAndRotation(easedAt[i], easedTurned[i]);
                }
            }
        }
    }

    private void PutBackPosed()
    {
        for (int i = 0; i < real.Length; i++)
        {
            real[i].SetLocalPositionAndRotation(posedAt[i], posedTurned[i]);
        }
    }
}
