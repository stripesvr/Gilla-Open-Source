// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ShopScroll : MonoBehaviour
{
    [Tooltip("How fast the texture goes up it (its height a second).")]
    public float speed = 0.18f;
    [Tooltip("How fast it goes along it, round a band (its width a second).")]
    public float sideways;
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private Renderer r;
    private MaterialPropertyBlock block;
    private Vector4 st;

    private void Start()
    {
        r = GetComponent<Renderer>();
        Material mat = r.sharedMaterial;
        Vector4 v = new Vector4(1f, 1f, 0f, 0f);
        if (mat != null)
        {
            if (mat.HasProperty(BaseMapST))
            {
                v = mat.GetVector(BaseMapST);
            }
        }
        st = v;
        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float w = st.w - speed * Time.deltaTime;
        w -= (int)w;
        st.w = Mathf.Clamp01(w);
        float z = st.z + sideways * Time.deltaTime;
        z -= (int)z;
        st.z = Mathf.Clamp01(z);
        r.GetPropertyBlock(block);
        block.SetVector(BaseMapST, st);
        r.SetPropertyBlock(block);
    }
}
