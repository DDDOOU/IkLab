using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IkLabUnity.EditorTools
{
    /// <summary>菜单：Tools → IkLab → 一键搭建演示场景。</summary>
    public static class IkLabSetup
    {
        [MenuItem("Tools/IkLab/一键搭建演示场景")]
        public static void BuildDemoScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 正交相机（正视 XY 平面）
            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.085f, 0.095f, 0.125f);
            camGo.AddComponent<PlanarCameraController>();
            camGo.tag = "MainCamera";

            // 平行光（用于球体着色）
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // 主控制器
            GameObject ikGo = new GameObject("IKLab");
            ikGo.AddComponent<IkController>();

            // 保存场景
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(), "Assets/Scenes/IkLab.unity");
            AssetDatabase.Refresh();

            Debug.Log("[IkLab] 场景已搭建并保存到 Assets/Scenes/IkLab.unity，点击 Play 即可拖拽末端。");
        }
    }
}
