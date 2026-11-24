// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MicLevel : MonoBehaviour
{
    public enum MicState
    {
        Starting = 0,
        NoPermission = 1,
        NoDevice = 2,
        StartFailed = 3,
        Stalled = 4,
        SilentInput = 5,
        Running = 6
    }

    [Header("Range (dBFS)")]
    [Tooltip("Below this you count as silent. The Quest mic runs quiet, hence the low floor.")]
    public float minDb = -58f;
    [Tooltip("At this and above the face is at full stretch (shouting).")]
    public float maxDb = -20f;
    [Header("Response")]
    [Tooltip("How fast the face opens when you start talking.")]
    public float attack = 25f;
    [Tooltip("How fast it settles back when you stop.")]
    public float release = 8f;
    public int sampleWindow = 512;
    [Header("Testing without a mic")]
    public bool useKeyboardFallback = true;
    public KeyCode testKey = KeyCode.V;
    public float ExternalLevel = -1f;
    public bool externalOnly;
    private AudioClip clip;
    private string device;
    private float[] buffer;
    private bool permissionAsked;
    private int lastPos = -1;
    private float posStuckFor;
    private float retryIn;
    private int rateIndex;
    private float zeroFor;
    private KeyCode mappedFrom = (KeyCode)(-1);
    private Key mappedKey = Key.V;

    public float Level { get; private set; }
    public float RawLevel { get; private set; }
    public float Db { get; private set; }
    public MicState State { get; private set; }

    private void Awake()
    {

        int n = sampleWindow;
        if (n < 65)
        {
            n = 64;
        }
        buffer = new float[n];
    }

    private void Start()
    {
        TryStartMic();
    }

    public void Restart()
    {
        externalOnly = false;
        ExternalLevel = -1f;
        TryStartMic();
    }

    private void TryStartMic()
    {
        int minFreq = 0;
        int maxFreq = 0;
        StopMic();
        if (externalOnly)
        {
            retryIn = 0f;
            State = MicState.Running;
            return;
        }
        if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            State = MicState.NoPermission;
            if (!permissionAsked)
            {
                permissionAsked = true;
                Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
            }
            retryIn = 1f;
            return;
        }
        if (Microphone.devices.Length == 0)
        {
            State = MicState.NoDevice;
            retryIn = 2f;
            Debug.LogWarning("[MicLevel] no microphone device");
            return;
        }
        device = Microphone.devices[0];
        Microphone.GetDeviceCaps(device, out minFreq, out maxFreq);
        int[] rates = new int[] { 0, 48000, 44100, 16000 } /* ? */;
        rates[0] = AudioSettings.outputSampleRate;
        int frequency = rates[rateIndex % rates.Length];
        if (minFreq != 0 || maxFreq != 0)
        {
            int clamped = frequency;
            if (maxFreq <= frequency)
            {
                clamped = maxFreq;
            }
            if (minFreq <= frequency)
            {
                frequency = clamped;
            }
            else
            {
                frequency = minFreq;
            }
        }
        clip = Microphone.Start(device, true, 1, frequency);
        if (clip == null)
        {
            State = MicState.StartFailed;
            retryIn = 2f;
            rateIndex++;
            Debug.LogWarning(string.Format("[MicLevel] Microphone.Start failed on '{0}' at {1}Hz (caps {2}-{3})", new object[] { device, frequency, minFreq, maxFreq }));
            return;
        }
        lastPos = -1;
        posStuckFor = 0f;
        State = MicState.Running;
        Debug.Log(string.Format("[MicLevel] listening on '{0}' at {1}Hz (caps {2}-{3}, sys {4})", new object[] { device, frequency, minFreq, maxFreq, AudioSettings.outputSampleRate }));
    }

    private void StopMic()
    {
        if (!string.IsNullOrEmpty(device) && Microphone.IsRecording(device))
        {
            Microphone.End(device);
        }
        clip = null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        if (!externalOnly)
        {
            if (ExternalLevel >= 0f)
            {
                RawLevel = Mathf.Min(ExternalLevel, 1f);
            }
            else
            {
                if (retryIn > 0f)
                {
                    retryIn -= dt;
                    if (retryIn <= 0f)
                    {
                        TryStartMic();
                    }
                }
                if (clip != null)
                {
                    int pos = Microphone.GetPosition(device);
                    if (pos == lastPos)
                    {
                        posStuckFor += dt;
                        if (posStuckFor > 2f)
                        {
                            State = MicState.Stalled;
                            Debug.LogWarning("[MicLevel] microphone is not delivering samples - trying the next sample rate");
                            rateIndex++;
                            posStuckFor = 0f;
                            retryIn = 1f;
                        }
                    }
                    else
                    {
                        lastPos = pos;
                        posStuckFor = 0f;
                        if (State == MicState.Stalled)
                        {
                            State = MicState.Running;
                        }
                    }
                    if (pos >= buffer.Length)
                    {
                        clip.GetData(buffer, pos - buffer.Length);
                        float sum = 0f;
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            sum += buffer[i] * buffer[i];
                        }
                        float rms = Mathf.Sqrt(sum / buffer.Length);
                        float db = Mathf.Log10(rms) * 20f;
                        Db = db;
                        float raw = 0f;
                        if (minDb != maxDb)
                        {
                            float t = (db - minDb) / (maxDb - minDb);
                            if (t >= 0f)
                            {
                                raw = t <= 1f ? t : 1f;
                            }
                        }
                        RawLevel = raw;
                        if (rms >= 1e-06f)
                        {
                            zeroFor = 0f;
                            if (State == MicState.SilentInput)
                            {
                                State = MicState.Running;
                            }
                        }
                        else
                        {
                            zeroFor += dt;
                            if (zeroFor > 3f && State == MicState.Running)
                            {
                                State = MicState.SilentInput;
                            }
                        }
                    }
                }
                else
                {
                    RawLevel = 0f;
                }
                if (useKeyboardFallback)
                {
                    Keyboard keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        KeyControl key = keyboard[TestKeyMapped];
                        if (key.isPressed)
                        {
                            float raw = RawLevel;
                            float wave = Mathf.Sin(Time.time * 7f) * 0.5f + 0.5f;
                            if (raw <= wave)
                            {
                                raw = wave;
                            }
                            RawLevel = raw;
                        }
                    }
                }
            }
        }
        else
        {
            if (clip != null)
            {
                StopMic();
            }
            RawLevel = Mathf.Clamp(ExternalLevel, 0f, 1f);
        }
        float level = Level;
        float target = RawLevel;
        float rate = target > level ? attack : release;
        float k = Mathf.Clamp01(1f - Mathf.Exp(-(rate * dt)));
        Level = level + (target - level) * k;
    }

    private Key TestKeyMapped
    {
        get
        {
            if (testKey != mappedFrom)
            {
                mappedFrom = testKey;
                if (!Enum.TryParse<Key>(testKey.ToString(), out mappedKey))
                {
                    mappedKey = Key.V;
                }
            }
            return mappedKey;
        }
    }

    private void OnDisable()
    {
        StopMic();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && isActiveAndEnabled)
        {
            TryStartMic();
        }
    }
}
