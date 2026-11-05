using UnityEngine;

/// <summary>跨 Unity 版本的对象查找辅助（2023+ 与旧版本 API 差异）。</summary>
public static class ObjectFinder
{
    public static T FindFirst<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
