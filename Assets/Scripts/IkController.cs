using System.Collections.Generic;
using IkLab;
using UnityEngine;

namespace IkLabUnity
{
    /// <summary>
    /// IK 关节链演示主组件：
    /// 根节点固定在原点；用户可拖拽末端手柄；CCD / FABRIK 反解各关节角度。
    /// </summary>
    public class IkController : MonoBehaviour
    {
        [Header("关节链设置")]
        [Range(2, 8)] public int jointCount = 4;
        public float segmentLength = 1f;

        [Header("IK 求解")]
        public bool useFabrik = false;
        public int maxIterations = 64;
        [Range(0.0001f, 0.05f)] public float tolerance = 0.002f;

        [Header("交互")]
        public float pickRadiusPx = 28f;
        public bool showHud = true;

        [Header("颜色")]
        public Color boneColor = new Color(0.28f, 0.72f, 1f);
        public Color jointColor = new Color(0.92f, 0.92f, 0.96f);
        public Color rootColor = new Color(1f, 0.75f, 0.2f);
        public Color endColor = new Color(0.2f, 1f, 0.7f);
        public Color targetColor = new Color(1f, 0.35f, 0.55f);
        public Color reachColor = new Color(0.62f, 0.62f, 0.68f, 0.28f);
        public Color gridColor = new Color(0.4f, 0.4f, 0.46f, 0.55f);

        // ---- 运行态 ----
        private IkChain2D chain;
        private Vec2 target = new Vec2(4f, 0f);
        private bool dragging;
        private bool dirty = true;
        private bool solved;
        private float endError;
        private float[] segAngles;
        private float[] jointAngles;

        // ---- 场景对象 ----
        private Camera cam;
        private LineRenderer boneLine;
        private LineRenderer reachLine;
        private readonly List<GameObject> jointSpheres = new List<GameObject>();
        private GameObject rootSphere;
        private GameObject endSphere;
        private GameObject targetSphere;
        private readonly List<Material> mats = new List<Material>();
        private Material matBone, matJoint, matRoot, matEnd, matTarget, matReach, matGrid;
        private Transform gridRoot;
        private GUIStyle titleStyle;

        private const int JointMin = 2;
        private const int JointMax = 8;

        private void Start()
        {
            cam = Camera.main != null ? Camera.main : ObjectFinder.FindFirst<Camera>();
            CreateMaterials();
            BuildChain(true);
            CreateStaticVisuals();
            ApplyPose();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < mats.Count; i++)
                if (mats[i] != null) Destroy(mats[i]);
            mats.Clear();
        }

        private void Update()
        {
            HandleKeys();
            HandleMouse();
            if (dirty)
                SolveAndApply();
        }

        // ===============================================================
        // 关节链与求解
        // ===============================================================
        private void BuildChain(bool resetPose)
        {
            int n = Mathf.Clamp(jointCount, JointMin, JointMax);
            jointCount = n;
            chain = new IkChain2D();
            chain.Build(n, Mathf.Max(0.1f, segmentLength), Vec2.Zero);
            if (resetPose)
                target = new Vec2(chain.TotalLength, 0f);
            EnsureJointSpheres(n);
            dirty = true;
        }

        private void SolveAndApply()
        {
            if (chain == null) return;

            if (useFabrik)
                solved = chain.SolveFabrik(target, maxIterations, tolerance);
            else
                solved = chain.SolveCcd(target, maxIterations, tolerance);

            endError = chain.EndDistanceTo(target);
            segAngles = chain.SegmentAnglesDeg();
            jointAngles = chain.JointAnglesDeg();
            dirty = false;
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (chain == null || boneLine == null) return;

            int n = chain.JointCount;
            boneLine.positionCount = n + 1;
            for (int i = 0; i <= n; i++)
                boneLine.SetPosition(i, ToWorld(chain.Points[i]));

            for (int i = 1; i < n; i++)
            {
                if (i - 1 < jointSpheres.Count && jointSpheres[i - 1] != null)
                    jointSpheres[i - 1].transform.position = ToWorld(chain.Points[i]);
            }

            if (rootSphere != null) rootSphere.transform.position = ToWorld(chain.Points[0]);
            if (endSphere != null) endSphere.transform.position = ToWorld(chain.End);
            if (targetSphere != null) targetSphere.transform.position = ToWorld(target);

            UpdateReachCircle();

            // 末端手柄：拖拽时高亮
            if (endSphere != null)
            {
                Renderer r = endSphere.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = dragging ? matTarget : matEnd;
            }
        }

        private void UpdateReachCircle()
        {
            if (reachLine == null || chain == null) return;
            float radius = chain.TotalLength;
            int seg = 72;
            reachLine.positionCount = seg + 1;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * 2f * i / seg;
                reachLine.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }

        // ===============================================================
        // 输入
        // ===============================================================
        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                target = new Vec2(chain.TotalLength, 0f);
                dirty = true;
            }
            if (Input.GetKeyDown(KeyCode.H))
                showHud = !showHud;
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsNearHandle(Input.mousePosition))
                {
                    dragging = true;
                    target = MouseOnPlane();
                    dirty = true;
                }
            }

            if (dragging)
            {
                if (Input.GetMouseButton(0))
                {
                    target = MouseOnPlane();
                    dirty = true;
                }
                else
                {
                    dragging = false;
                    dirty = true;
                }
            }
        }

        private bool IsNearHandle(Vector3 mousePos)
        {
            if (cam == null) return false;
            if (endSphere != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(endSphere.transform.position);
                if (sp.z > 0f && (sp - mousePos).magnitude <= pickRadiusPx) return true;
            }
            if (targetSphere != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(targetSphere.transform.position);
                if (sp.z > 0f && (sp - mousePos).magnitude <= pickRadiusPx) return true;
            }
            return false;
        }

        private Vec2 MouseOnPlane()
        {
            if (cam == null) return target;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            float enter;
            if (plane.Raycast(ray, out enter))
            {
                Vector3 p = ray.GetPoint(enter);
                return new Vec2(p.x, p.y);
            }
            return target;
        }

        // ===============================================================
        // 场景对象
        // ===============================================================
        private void CreateStaticVisuals()
        {
            boneLine = MakeLine("Bones", matBone, 0.16f, 5);
            boneLine.numCapVertices = 4;
            boneLine.positionCount = chain.JointCount + 1;

            reachLine = MakeLine("ReachCircle", matReach, 0.045f, 0);
            reachLine.loop = true;
            reachLine.numCapVertices = 0;

            rootSphere = MakeSphere("Root", matRoot, 0.34f);
            endSphere = MakeSphere("EndEffector", matEnd, 0.28f);
            targetSphere = MakeSphere("TargetHandle", matTarget, 0.18f);

            CreateGrid();
        }

        private void EnsureJointSpheres(int n)
        {
            int need = Mathf.Max(0, n - 1);
            while (jointSpheres.Count < need)
                jointSpheres.Add(null);

            for (int i = 0; i < jointSpheres.Count; i++)
            {
                if (i < need && jointSpheres[i] == null)
                    jointSpheres[i] = MakeSphere("Joint" + (i + 1), matJoint, 0.22f);
                if (i >= need && jointSpheres[i] != null)
                {
                    Destroy(jointSpheres[i]);
                    jointSpheres[i] = null;
                }
            }
            while (jointSpheres.Count > need)
                jointSpheres.RemoveAt(jointSpheres.Count - 1);
        }

        private LineRenderer MakeLine(string name, Material m, float width, int sortOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = m;
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = sortOrder;
            lr.alignment = LineAlignment.View;
            return lr;
        }

        private GameObject MakeSphere(string name, Material m, float scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
            return go;
        }

        private void CreateMaterials()
        {
            matBone = MakeMat(boneColor);
            matJoint = MakeMat(jointColor);
            matRoot = MakeMat(rootColor);
            matEnd = MakeMat(endColor);
            matTarget = MakeMat(targetColor);
            matReach = MakeMat(reachColor);
            matGrid = MakeMat(gridColor);
        }

        private Material MakeMat(Color c)
        {
            string[] shaders = { "Unlit/Color", "Universal Render Pipeline/Unlit", "Standard" };
            Material m = null;
            for (int i = 0; i < shaders.Length && m == null; i++)
            {
                Shader s = Shader.Find(shaders[i]);
                if (s != null) m = new Material(s);
            }
            if (m == null) m = new Material(Shader.Find("Sprites/Default"));

            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.25f);
            mats.Add(m);
            return m;
        }

        private static Vector3 ToWorld(Vec2 v)
        {
            return new Vector3(v.X, v.Y, 0f);
        }

        private void CreateGrid()
        {
            GameObject root = new GameObject("Grid");
            root.transform.SetParent(transform, false);
            gridRoot = root.transform;

            int extent = 8;
            for (int i = -extent; i <= extent; i++)
            {
                LineRenderer v = MakeLine("GridV" + i, matGrid, 0.02f, -1);
                v.transform.SetParent(gridRoot, false);
                v.positionCount = 2;
                v.SetPosition(0, new Vector3(i, -extent, 0f));
                v.SetPosition(1, new Vector3(i, extent, 0f));

                LineRenderer h = MakeLine("GridH" + i, matGrid, 0.02f, -1);
                h.transform.SetParent(gridRoot, false);
                h.positionCount = 2;
                h.SetPosition(0, new Vector3(-extent, i, 0f));
                h.SetPosition(1, new Vector3(extent, i, 0f));
            }
        }

        // ===============================================================
        // HUD
        // ===============================================================
        private void OnGUI()
        {
            if (!showHud || chain == null) return;

            GUI.Box(new Rect(10f, 10f, 408f, 548f), GUIContent.none, GUI.skin.box);
            GUILayout.BeginArea(new Rect(24f, 20f, 380f, 528f));

            GUILayout.Label("IK 关节链演示（2D 平面链）", TitleStyle());
            GUILayout.Label("根节点固定 · 拖拽末端 · IK 反解各关节角度");

            GUILayout.Space(6f);
            GUILayout.Label("关节数（段数）：");
            int count = GUILayout.SelectionGrid(jointCount - JointMin,
                new string[] { "2", "3", "4", "5", "6", "7", "8" }, 7, GUILayout.Height(26f)) + JointMin;
            if (count != jointCount)
            {
                jointCount = count;
                BuildChain(false);
            }

            GUILayout.Space(6f);
            GUILayout.Label("IK 算法：");
            int alg = GUILayout.SelectionGrid(useFabrik ? 1 : 0,
                new string[] { "CCD（坐标下降）", "FABRIK（前后向迭代）" }, 2, GUILayout.Height(26f));
            if ((alg == 1) != useFabrik)
            {
                useFabrik = alg == 1;
                dirty = true;
            }

            GUILayout.Space(8f);
            GUILayout.Label("总长 " + chain.TotalLength.ToString("F2") + "    可达半径 = 总长");
            GUILayout.Label("目标 " + Fmt(target) + "      末端 " + Fmt(chain.End));
            GUILayout.Label("末端误差 " + endError.ToString("F4"));

            bool reachable = solved;
            GUI.color = reachable ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.55f, 0.45f);
            GUILayout.Label(reachable ? "状态：可达（末端已贴合目标）" : "状态：超出可达范围（链已伸直指向目标）");
            GUI.color = Color.white;

            GUILayout.Space(6f);
            GUILayout.Label("各关节角度（度）：");
            if (segAngles != null && jointAngles != null)
            {
                for (int i = 0; i < chain.JointCount; i++)
                {
                    string rel = i == 0
                        ? "根关节 绝对 " + Signed(jointAngles[0])
                        : "关节 " + i + " 相对 " + Signed(jointAngles[i]);
                    GUILayout.Label("  " + rel + "    段角 " + Signed(segAngles[i]));
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label("左键拖拽末端/目标小球移动；R 重置姿态；H 隐藏面板");
            GUILayout.Label("滚轮缩放 · 中键平移 · F 重置视角（相机在相机组件上）");
            GUILayout.Label("蓝线=骨骼  白球=中间关节  金球=固定根  绿球=末端  红球=目标");

            GUILayout.EndArea();
        }

        private static string Signed(float deg)
        {
            return (deg >= 0f ? "+" : "") + deg.ToString("F2") + "°";
        }

        private static string Fmt(Vec2 v)
        {
            return "(" + v.X.ToString("F2") + ", " + v.Y.ToString("F2") + ")";
        }

        private GUIStyle TitleStyle()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 19;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = new Color(0.55f, 0.85f, 1f);
            }
            return titleStyle;
        }
    }
}
