using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 物品图标图集加载器。
///
/// 为什么需要：
///   10 张独立 PNG 会让 UGUI 断批，每张纹理至少一个批次；
///   合并进 SpriteAtlas 后，同屏图标可以共享一张纹理、合成一个批次。
///
/// 约定：
///   ItemConfig 里仍然写 "ItemIcons/63_31100063"；
///   这里只取文件名作为 Sprite 名，从图集里 GetSprite，避免改配置表。
/// </summary>
public static class IconAtlasLoader
{
    private static SpriteAtlas atlas;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string pathOrName)
    {
        if (string.IsNullOrEmpty(pathOrName))
        {
            return null;
        }

        string spriteName = pathOrName;
        int slash = spriteName.LastIndexOf('/');
        if (slash >= 0)
        {
            spriteName = spriteName.Substring(slash + 1);
        }
        if (spriteName.EndsWith(".png"))
        {
            spriteName = spriteName.Substring(0, spriteName.Length - 4);
        }

        Sprite cached;
        if (cache.TryGetValue(spriteName, out cached))
        {
            return cached;
        }

        if (atlas == null)
        {
            atlas = Resources.Load<SpriteAtlas>("ItemIconAtlas");
        }

        Sprite sprite = atlas != null ? atlas.GetSprite(spriteName) : null;
        cache[spriteName] = sprite;
        return sprite;
    }

    /// <summary>切换图集/热更后清缓存（当前未使用，预留给资源热更）。</summary>
    public static void ClearCache()
    {
        cache.Clear();
        atlas = null;
    }
}
