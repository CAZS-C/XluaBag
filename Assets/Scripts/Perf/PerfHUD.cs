using UnityEngine;
using Unity.Profiling;

/// <summary>
/// 运行时性能 HUD（F1 开关）。
/// 显示 FPS、帧时间、GC Alloc、DrawCall、Batches、SetPass、Triangles。
/// 只在编辑器和 Development Build 中显示，正式包不输出。
/// </summary>
public class PerfHUD : MonoBehaviour
{
    private bool visible = true;
    private float fps;
    private float frameMs;
    private float frameMsMax;
    private float[] samples = new float[60];
    private int sampleIndex;

    private ProfilerRecorder drawCalls;
    private ProfilerRecorder batches;
    private ProfilerRecorder setPassCalls;
    private ProfilerRecorder triangles;
    private ProfilerRecorder gcAlloc;

    private GUIStyle style;

    private void Awake()
    {
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            visible = !visible;
        }

        float delta = Time.unscaledDeltaTime;
        fps = delta > 0f ? 1f / delta : 0f;
        frameMs = delta * 1000f;

        samples[sampleIndex] = frameMs;
        sampleIndex = (sampleIndex + 1) % samples.Length;

        float max = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            if (samples[i] > max) max = samples[i];
        }
        frameMsMax = max;
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!visible) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 13;
            style.normal.textColor = Color.white;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.Box(new Rect(8, 8, 270, 150), GUIContent.none);
        GUI.color = Color.white;

        string text =
            "FPS: " + fps.ToString("F1") + "   帧: " + frameMs.ToString("F1") + " ms (max " + frameMsMax.ToString("F1") + ")\n" +
            "GC Alloc: " + FormatCounter(gcAlloc, "B") + "\n" +
            "DrawCall: " + FormatCounter(drawCalls, "") + "\n" +
            "Batches: " + FormatCounter(batches, "") + "\n" +
            "SetPass: " + FormatCounter(setPassCalls, "") + "\n" +
            "Triangles: " + FormatCounter(triangles, "") + "\n" +
            "F1 显示/隐藏";

        GUI.Label(new Rect(16, 14, 250, 140), text, style);
#endif
    }

    private static string FormatCounter(ProfilerRecorder recorder, string unit)
    {
        if (!recorder.Valid) return "n/a";
        return recorder.LastValue.ToString("N0") + unit;
    }

    private void OnDestroy()
    {
        drawCalls.Dispose();
        batches.Dispose();
        setPassCalls.Dispose();
        triangles.Dispose();
        gcAlloc.Dispose();
    }
}
