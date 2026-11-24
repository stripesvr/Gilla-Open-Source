// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

[RequireComponent(typeof(Renderer))]
public class MirrorSurface : MonoBehaviour
{
    [Tooltip("Size of each eye's mirror picture in the editor and on a monitor (long side).")]
    public int textureSize = 1024;
    [Header("On the headset")]
    [Tooltip("Each eye's mirror picture on a Quest 3 (long side).")]
    public int quest3TextureSize = 1536;
    [Tooltip("The same on a Quest 2.")]
    public int quest2TextureSize = 1024;
    [Tooltip("Re-render every Nth frame on a Quest 3. In between, the glass keeps looking up the last pictures with the matrices they were made with, so the reflection stays put in the world. (Every frame: a players-only mirror is a cheap render, and every other frame it trailed your moves - user 10-30, after Blud Slop's, which re-renders each eye every frame.)")]
    public int quest3FrameSkip = 1;
    [Tooltip("The same on a Quest 2.")]
    public int quest2FrameSkip = 2;
    [Tooltip("Anti-aliasing samples for the mirror picture (1 = none): smooth edges on the reflected players.")]
    public int msaa = 4;
    [Tooltip("The same on a Quest 2.")]
    public int quest2Msaa = 2;
    [Tooltip("Stop rendering the mirror when the player is further than this from it (it keeps its last image).")]
    public float maxDistance = 6f;
    [Tooltip("Layers the mirror can see. Leave out the first-person avatar layer.")]
    public LayerMask reflectionMask = -1;
    public float farClip = 60f;
    [Tooltip("The reflection's near plane sits this far in front of the glass, so nothing pokes through it.")]
    public float clipOffset = 0.01f;
    [Tooltip("Clear the reflection to see-through black instead of the sky, so the glass's shader can fill what isn't reflected (the void mirror's sparkles). Give it a mask of only the players.")]
    public bool playersOnly;

    private static readonly int TexL = Shader.PropertyToID("_MirrorTexL");
    private static readonly int TexR = Shader.PropertyToID("_MirrorTexR");
    private static readonly int LookupL = Shader.PropertyToID("_MirrorLookupL");
    private static readonly int LookupR = Shader.PropertyToID("_MirrorLookupR");
    private static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

    private Camera mirrorCam;
    private readonly RenderTexture[] targets = new RenderTexture[2];
    private readonly Matrix4x4[] views = new Matrix4x4[2];
    private readonly Matrix4x4[] projections = new Matrix4x4[2];
    private readonly Matrix4x4[] lookups = new Matrix4x4[2];
    private readonly Plane[] planes = new Plane[6];
    private MaterialPropertyBlock block;
    private Renderer glass;
    private int size;
    private int skip = 1;
    private int frame;
    private int samples = 1;
    private bool hooked;

    private void Start()
    {
        Init();
        NameTag.MakeYours();
        Application.onBeforeRender += BeforeRender;
        hooked = true;
    }

    private void OnEnable()
    {

        if (!hooked && mirrorCam != null)
        {
            Application.onBeforeRender += BeforeRender;
            hooked = true;
        }
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= BeforeRender;
        hooked = false;
    }

    private void Init()
    {
        if (block == null)
            block = new MaterialPropertyBlock();
        if (mirrorCam != null)
            return;

        bool mobile = Application.isMobilePlatform;
        string device = (SystemInfo.deviceName + " " + SystemInfo.deviceModel).ToLowerInvariant();
        bool quest2 = device.Contains("quest 2") || device.Contains("quest2");
        int frameSkip;
        if (mobile)
        {
            size = quest2 ? quest2TextureSize : quest3TextureSize;
            frameSkip = quest2 ? quest2FrameSkip : quest3FrameSkip;
            if (frameSkip < 2) frameSkip = 1;
        }
        else
        {
            size = textureSize;
            frameSkip = 1;
        }

        int aa = quest2 ? quest2Msaa : msaa;
        if (aa > 7) aa = 8;
        if (aa < 2) aa = 1;
        uint p = (uint)(aa - 1) | (uint)(aa - 1) >> 2;
        p |= p >> 1;
        uint up = p + 1;
        uint result = up >> 1;
        skip = frameSkip;
        if ((int)(up - aa) <= (int)(aa - (up >> 1)))
            result = up;
        samples = (int)result;

        glass = GetComponent<Renderer>();
        GameObject go = new GameObject("MirrorCamera");
        if (!Application.isPlaying)
            go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);
        mirrorCam = go.AddComponent<Camera>();
        mirrorCam.enabled = false;
        mirrorCam.cullingMask = reflectionMask.value;
        mirrorCam.nearClipPlane = 0.05f;
        mirrorCam.farClipPlane = farClip;
        mirrorCam.clearFlags = playersOnly ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        mirrorCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        mirrorCam.useOcclusionCulling = false;
        mirrorCam.allowMSAA = samples > 1;
        mirrorCam.allowHDR = false;
        UniversalAdditionalCameraData data = mirrorCam.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.antialiasing = AntialiasingMode.None;
            data.allowXRRendering = false;
            data.stopNaN = false;
            data.dithering = false;
        }
    }

    private void BeforeRender()
    {
        if (this == null || !isActiveAndEnabled)
            return;
        Camera camera = Camera.main;
        if (camera == null)
            return;
        Vector3 d = transform.position - camera.transform.position;
        if (maxDistance * maxDistance < d.sqrMagnitude)
            return;
        GeometryUtility.CalculateFrustumPlanes(camera, planes);
        if (!GeometryUtility.TestPlanesAABB(planes, glass.bounds))
            return;
        if (skip > 1)
        {
            frame++;
            if (frame % skip != 0)
                return;
        }
        RenderFor(camera);
    }

    public void RenderFor(Camera viewer)
    {
        Init();
        int eyes = Eyes(viewer);
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 n = -fwd;
        if (Vector3.Dot(fwd, viewer.transform.position - pos) >= 0f)
            n = fwd;
        Matrix4x4 reflection = Reflection(n, pos);

        for (int e = 0; e < eyes; e++)
        {
            Target(e, viewer);
            Matrix4x4 view = views[e] * reflection;
            Matrix4x4 proj = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)) * projections[e];
            Matrix4x4 camWorld = reflection * views[e].inverse;
            Vector3 forward = -(Vector3)camWorld.GetColumn(2);
            Vector3 up = camWorld.GetColumn(1);
            Quaternion rotation = Quaternion.LookRotation(forward, up);
            Vector3 camPos = camWorld.MultiplyPoint(Vector3.zero);
            mirrorCam.transform.SetPositionAndRotation(camPos, rotation);
            NameTag.FaceMirror(mirrorCam.transform.position);
            mirrorCam.worldToCameraMatrix = view;
            Vector4 clipPlane = CameraSpacePlane(view, pos + n * clipOffset, n);
            mirrorCam.projectionMatrix = Oblique(proj, clipPlane);
            mirrorCam.cullingMatrix = proj * view;
            mirrorCam.targetTexture = targets[e];
            mirrorCam.Render();
            lookups[e] = proj * view;
        }

        mirrorCam.targetTexture = null;
        NameTag.FaceViewer(viewer.transform.position);
        glass.GetPropertyBlock(block);
        int r = eyes > 1 ? 1 : 0;
        block.SetTexture(TexL, targets[0]);
        block.SetTexture(TexR, targets[r]);
        block.SetMatrix(LookupL, lookups[0]);
        block.SetMatrix(LookupR, lookups[r]);
        glass.SetPropertyBlock(block);
    }

    private int Eyes(Camera viewer)
    {
        if (viewer.stereoEnabled)
        {
            SubsystemManager.GetSubsystems(displays);
            int count = 0;
            foreach (XRDisplaySubsystem display in displays)
            {
                if (!display.running)
                    continue;
                for (int p = 0; p < display.GetRenderPassCount() && count < 2; )
                {
                    display.GetRenderPass(p, out XRDisplaySubsystem.XRRenderPass pass);
                    int k = 0;
                    while (k < pass.GetRenderParameterCount() && count + k <= 1)
                    {
                        pass.GetRenderParameter(viewer, k, out XRDisplaySubsystem.XRRenderParameter param);
                        views[count + k] = param.view;
                        projections[count + k] = param.projection;
                        k++;
                    }
                    p++;
                    count += k;
                }
                if (count >= 1)
                    break;
            }
            if (count == 2)
                return 2;
            views[0] = viewer.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            views[1] = viewer.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
            projections[0] = viewer.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            projections[1] = viewer.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            return 2;
        }
        views[0] = viewer.worldToCameraMatrix;
        projections[0] = viewer.projectionMatrix;
        return 1;
    }

    private void Target(int e, Camera viewer)
    {
        float aspect = viewer.aspect;
        float a;
        int width;
        int height;
        if (aspect < 0.3f) a = 0.3f;
        else if (aspect > 4f) a = 4f;
        else a = aspect;

        if (aspect < 0.3f || (aspect <= 4f && aspect < 1f))
        {
            width = Mathf.RoundToInt(a * size);
            height = size;
            if (width < 65) width = 64;
        }
        else
        {
            width = size;
            height = Mathf.RoundToInt(size / a);
            if (height < 65) height = 64;
        }

        RenderTexture current = targets[e];
        if (current != null && current.width == width && current.height == height)
            return;
        if (current != null)
        {
            current.Release();
            Kill(current);
        }
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.Default);
        rt.name = "Mirror" + (e != 0 ? "R" : "L");
        rt.antiAliasing = samples;
        rt.useMipMap = samples == 1;
        rt.autoGenerateMips = samples == 1;
        rt.filterMode = samples == 1 ? FilterMode.Trilinear : FilterMode.Bilinear;
        rt.anisoLevel = 2;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.hideFlags = HideFlags.DontSave;
        targets[e] = rt;
        targets[e].Create();
    }

    private static Matrix4x4 Reflection(Vector3 n, Vector3 point)
    {
        float d = 2f * Vector3.Dot(n, point);
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = 1f - 2f * n.x * n.x;
        m.m01 = -2f * n.y * n.x;
        m.m02 = -2f * n.z * n.x;
        m.m03 = n.x * d;
        m.m10 = -2f * n.x * n.y;
        m.m11 = 1f - 2f * n.y * n.y;
        m.m12 = -2f * n.z * n.y;
        m.m13 = n.y * d;
        m.m20 = -2f * n.x * n.z;
        m.m21 = -2f * n.y * n.z;
        m.m22 = 1f - 2f * n.z * n.z;
        m.m23 = n.z * d;
        return m;
    }

    private static Vector4 CameraSpacePlane(Matrix4x4 view, Vector3 point, Vector3 normal)
    {
        Vector3 p = view.MultiplyPoint(point);
        Vector3 n = view.MultiplyVector(normal).normalized;
        return new Vector4(n.x, n.y, n.z, -Vector3.Dot(p, n));
    }

    private static Matrix4x4 Oblique(Matrix4x4 proj, Vector4 clipPlane)
    {
        Matrix4x4 inv = proj.inverse;
        Vector4 cp = inv.transpose * clipPlane;
        Vector4 q = proj.inverse * new Vector4(cp.x >= 0f ? 1f : -1f, cp.y >= 0f ? 1f : -1f, 1f, 1f);
        Vector4 c = clipPlane * (2f / Vector4.Dot(clipPlane, q));
        proj.m20 = c.x - proj.m30;
        proj.m21 = c.y - proj.m31;
        proj.m22 = c.z - proj.m32;
        proj.m23 = c.w - proj.m33;
        return proj;
    }

    public void Release()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].Release();
                Kill(targets[i]);
                targets[i] = null;
            }
        }
        if (mirrorCam != null)
        {
            Kill(mirrorCam.gameObject);
            mirrorCam = null;
        }
        if (glass != null)
            glass.SetPropertyBlock(null);
    }

    private static void Kill(Object o)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(o);
            return;
        }
        Object.DestroyImmediate(o);
    }

    private void OnDestroy()
    {
        Application.onBeforeRender -= BeforeRender;
        Release();
    }
}
