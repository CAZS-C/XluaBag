using UnityEngine;

/// <summary>
/// 演示场景的占位美术工具。
///
/// 这个 Demo 不引入新的 PNG/材质资源，玩家、地面网格、品质光圈
/// 全部用运行时生成的小 Sprite，避免为了演示再走一遍美术导入/AB 流程。
/// 生成结果会静态缓存，场景重载后不会反复创建纹理。
/// </summary>
public static class DemoArt
{
    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite triangleSprite;
    private static Sprite gridSprite;
    private static Font uiFont;

    /// <summary>UI 字体：优先使用系统中文字体，避免 Arial 缺字时中文变方框</summary>
    public static Font GetUIFont()
    {
        if (uiFont != null) return uiFont;

        uiFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Arial" }, 18);

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return uiFont;
    }

    /// <summary>白色实心圆，靠 SpriteRenderer.color 染色</summary>
    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null) circleSprite = CreateCircle(64);
            return circleSprite;
        }
    }

    /// <summary>白色圆环，用于物品品质光圈</summary>
    public static Sprite RingSprite
    {
        get
        {
            if (ringSprite == null) ringSprite = CreateRing(64, 6f);
            return ringSprite;
        }
    }

    /// <summary>白色三角形，用于玩家朝向指示</summary>
    public static Sprite TriangleSprite
    {
        get
        {
            if (triangleSprite == null) triangleSprite = CreateTriangle(64);
            return triangleSprite;
        }
    }

    /// <summary>深色网格地面，Tiled 模式下按房间尺寸平铺</summary>
    public static Sprite GridSprite
    {
        get
        {
            if (gridSprite == null) gridSprite = CreateGrid(64);
            return gridSprite;
        }
    }

    private static Sprite CreateCircle(int size)
    {
        Texture2D tex = NewTexture(size);
        Color[] pixels = new Color[size * size];
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f),
                    new Vector2(radius, radius));
                // 边缘 1.5 像素做柔和过渡，避免锯齿明显
                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        return Finish(tex, pixels, size);
    }

    private static Sprite CreateRing(int size, float thickness)
    {
        Texture2D tex = NewTexture(size);
        Color[] pixels = new Color[size * size];
        float radius = size * 0.5f;
        float inner = radius - thickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f),
                    new Vector2(radius, radius));

                float alpha = 0f;
                if (dist <= radius && dist >= inner)
                {
                    // 内外边缘各留 1 像素过渡
                    alpha = Mathf.Clamp01(Mathf.Min(dist - inner, radius - dist) / 1f);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        return Finish(tex, pixels, size);
    }

    private static Sprite CreateTriangle(int size)
    {
        Texture2D tex = NewTexture(size);
        Color[] pixels = new Color[size * size];

        Vector2 a = new Vector2(0.5f * size, 0.95f * size); // 顶点朝上
        Vector2 b = new Vector2(0.10f * size, 0.10f * size);
        Vector2 c = new Vector2(0.90f * size, 0.10f * size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = PointInTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c);
                pixels[y * size + x] = inside
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 1f, 1f, 0f);
            }
        }
        return Finish(tex, pixels, size);
    }

    private static Sprite CreateGrid(int size)
    {
        Texture2D tex = NewTexture(size);
        Color[] pixels = new Color[size * size];

        Color background = new Color(0.09f, 0.10f, 0.13f, 1f);
        Color line = new Color(0.20f, 0.23f, 0.29f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onLine = (x % 32 == 0) || (y % 32 == 0);
                pixels[y * size + x] = onLine ? line : background;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        // FullRect 是 Tiled drawMode 能正常工作的前提
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Texture2D NewTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static Sprite Finish(Texture2D tex, Color[] pixels, int size)
    {
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
