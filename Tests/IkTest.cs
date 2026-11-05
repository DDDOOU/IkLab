using System;
using IkLab;

namespace IkLabTests
{
    internal static class IkTest
    {
        private static int pass = 0;
        private static int fail = 0;

        private static void Check(string name, bool cond)
        {
            if (cond) { pass++; Console.WriteLine("PASS  " + name); }
            else { fail++; Console.WriteLine("FAIL  " + name); }
        }

        private static void Near(string name, float actual, float expected, float eps)
        {
            bool ok = Math.Abs(actual - expected) <= eps;
            if (!ok)
                Console.WriteLine("      expected " + expected.ToString("F6") + "  actual " + actual.ToString("F6"));
            Check(name, ok);
        }

        private static bool Finite(Vec2 v)
        {
            return !float.IsNaN(v.X) && !float.IsNaN(v.Y) &&
                   !float.IsInfinity(v.X) && !float.IsInfinity(v.Y);
        }

        private static bool AllFinite(Vec2[] pts)
        {
            for (int i = 0; i < pts.Length; i++)
                if (!Finite(pts[i])) return false;
            return true;
        }

        private static bool LengthsOk(IkChain2D c, float eps)
        {
            for (int i = 0; i < c.JointCount; i++)
            {
                float l = (c.Points[i + 1] - c.Points[i]).Length;
                if (Math.Abs(l - c.SegmentLength) > eps) return false;
            }
            return true;
        }

        private static void Main()
        {
            Console.WriteLine("=== IkLab 离线测试 ===");

            // ---------- 构链 ----------
            IkChain2D c = new IkChain2D();
            c.Build(4, 1f, Vec2.Zero);
            Check("Build: 点数 = 关节数+1", c.Points.Length == 5);
            Check("Build: 根在原点", c.Points[0].X == 0f && c.Points[0].Y == 0f);
            Near("Build: 末端 X = 4", c.End.X, 4f, 1e-5f);
            Near("Build: 末端 Y = 0", c.End.Y, 0f, 1e-5f);
            Near("Build: 总长 = 4", c.TotalLength, 4f, 1e-5f);
            Check("Build: 各段等长", LengthsOk(c, 1e-5f));

            c.Build(3, 1.5f, new Vec2(1f, 2f));
            Check("Build: 自定义根", Math.Abs(c.Points[0].X - 1f) < 1e-5f && Math.Abs(c.Points[0].Y - 2f) < 1e-5f);
            Near("Build: 自定义根+末端", c.End.X, 1f + 4.5f, 1e-4f);

            // ---------- CCD 可达 ----------
            c.Build(3, 1f, Vec2.Zero);
            bool ok = c.SolveCcd(new Vec2(2f, 1f), 64, 1e-3f);
            Check("CCD: 可达目标收敛", ok && c.EndDistanceTo(new Vec2(2f, 1f)) < 1e-3f);
            Check("CCD: 骨长保持", LengthsOk(c, 1e-3f));
            Check("CCD: 根固定", (c.Points[0] - c.Root).Length < 1e-6f);

            c.Build(8, 1f, Vec2.Zero);
            ok = c.SolveCcd(new Vec2(5f, 3f), 64, 1e-3f);
            Check("CCD: 8 关节可达目标", ok && c.EndDistanceTo(new Vec2(5f, 3f)) < 1e-3f);
            Check("CCD: 8 关节骨长保持", LengthsOk(c, 1e-3f));

            // ---------- CCD 超出可达 ----------
            c.Build(3, 1f, Vec2.Zero);
            ok = c.SolveCcd(new Vec2(5f, 0f), 64, 1e-3f);
            Near("CCD: 超出可达时末端 X=总长", c.End.X, 3f, 1e-5f);
            Near("CCD: 超出可达时末端 Y=0", c.End.Y, 0f, 1e-5f);
            Check("CCD: 超出可达返回未解", !ok);

            ok = c.SolveCcd(new Vec2(3f, 0f), 64, 1e-3f);
            Check("CCD: 恰在可达边界", ok && c.EndDistanceTo(new Vec2(3f, 0f)) < 1e-4f);

            // ---------- FABRIK 可达 ----------
            c.Build(3, 1f, Vec2.Zero);
            ok = c.SolveFabrik(new Vec2(2f, 1f), 32, 1e-3f);
            Check("FABRIK: 可达目标收敛", ok && c.EndDistanceTo(new Vec2(2f, 1f)) < 1e-3f);
            Check("FABRIK: 骨长保持", LengthsOk(c, 1e-3f));
            Check("FABRIK: 根固定", (c.Points[0] - c.Root).Length < 1e-6f);

            c.Build(8, 1f, Vec2.Zero);
            ok = c.SolveFabrik(new Vec2(5f, 3f), 32, 1e-3f);
            Check("FABRIK: 8 关节可达目标", ok && c.EndDistanceTo(new Vec2(5f, 3f)) < 1e-3f);

            // ---------- FABRIK 超出可达 ----------
            c.Build(3, 1f, Vec2.Zero);
            ok = c.SolveFabrik(new Vec2(5f, 0f), 32, 1e-3f);
            Near("FABRIK: 超出可达时末端 X=总长", c.End.X, 3f, 1e-5f);
            Check("FABRIK: 超出可达返回未解", !ok);

            // ---------- FABRIK 退化：目标=根 ----------
            c.Build(3, 1f, Vec2.Zero);
            c.SolveFabrik(Vec2.Zero, 32, 1e-3f);
            Check("FABRIK: 目标=根 无 NaN", AllFinite(c.Points));
            Check("FABRIK: 目标=根 骨长保持", LengthsOk(c, 1e-4f));

            // ---------- 单关节 ----------
            // 单关节链只有 1 段骨，末端只能落在半径 = 骨长 的圆上
            c.Build(1, 1f, Vec2.Zero);
            ok = c.SolveCcd(new Vec2(0.6f, 0.8f), 64, 1e-3f);
            Check("CCD: 单关节可达（圆上）", ok && c.EndDistanceTo(new Vec2(0.6f, 0.8f)) < 1e-3f);

            c.Build(1, 1f, Vec2.Zero);
            ok = c.SolveCcd(new Vec2(0.3f, 0.4f), 64, 1e-3f);
            Check("CCD: 单关节圆内不可达", !ok);
            Near("CCD: 单关节末端仍在圆上", c.End.Length, 1f, 1e-3f);

            c.Build(1, 1f, Vec2.Zero);
            ok = c.SolveFabrik(new Vec2(0.6f, 0.8f), 64, 1e-3f);
            Check("FABRIK: 单关节可达（圆上）", ok && c.EndDistanceTo(new Vec2(0.6f, 0.8f)) < 1e-3f);

            c.Build(1, 1f, Vec2.Zero);
            ok = c.SolveFabrik(new Vec2(0.3f, 0.4f), 64, 1e-3f);
            Check("FABRIK: 单关节圆内不可达", !ok);
            Near("FABRIK: 单关节末端仍在圆上", c.End.Length, 1f, 1e-3f);

            // ---------- 两种算法一致性（随机可达目标） ----------
            int bothOk = 0;
            Random rnd = new Random(202411581);
            for (int i = 0; i < 20; i++)
            {
                float ang = (float)(rnd.NextDouble() * Math.PI * 2);
                float rad = (float)(rnd.NextDouble() * 3.4f + 0.2f);
                Vec2 target = new Vec2((float)Math.Cos(ang) * rad, (float)Math.Sin(ang) * rad);

                IkChain2D a = new IkChain2D(); a.Build(4, 1f, Vec2.Zero);
                IkChain2D b = new IkChain2D(); b.Build(4, 1f, Vec2.Zero);
                bool oa = a.SolveCcd(target, 64, 1e-3f);
                bool ob = b.SolveFabrik(target, 64, 1e-3f);
                if (oa && ob && (a.End - b.End).Length < 1e-2f) bothOk++;
            }
            Check("CCD/FABRIK 末端一致 (20 例)", bothOk == 20);

            // ---------- 角度：伸直链 ----------
            c.Build(3, 1f, Vec2.Zero);
            float[] seg = c.SegmentAnglesDeg();
            float[] joints = c.JointAnglesDeg();
            Check("角度: 伸直链段角全 0", Math.Abs(seg[0]) < 1e-4f && Math.Abs(seg[1]) < 1e-4f && Math.Abs(seg[2]) < 1e-4f);
            Check("角度: 伸直链关节角全 0", Math.Abs(joints[0]) < 1e-4f && Math.Abs(joints[1]) < 1e-4f && Math.Abs(joints[2]) < 1e-4f);

            // ---------- 角度：手摆姿态 ----------
            c.Build(3, 1f, Vec2.Zero);
            c.Points[0] = new Vec2(0f, 0f);
            c.Points[1] = new Vec2(1f, 0f);
            c.Points[2] = new Vec2(1f, 1f);
            c.Points[3] = new Vec2(1f, 2f);
            seg = c.SegmentAnglesDeg();
            joints = c.JointAnglesDeg();
            Near("角度: 段 0 = 0°", seg[0], 0f, 1e-4f);
            Near("角度: 段 1 = 90°", seg[1], 90f, 1e-4f);
            Near("角度: 段 2 = 90°", seg[2], 90f, 1e-4f);
            Near("角度: 关节 0 = 0°", joints[0], 0f, 1e-4f);
            Near("角度: 关节 1 = 90°", joints[1], 90f, 1e-4f);
            Near("角度: 关节 2 = 0°", joints[2], 0f, 1e-4f);

            // ---------- 归一化 ----------
            Near("归一化: 190 -> -170", IkChain2D.NormalizeDeg(190f), -170f, 1e-4f);
            Near("归一化: -190 -> 170", IkChain2D.NormalizeDeg(-190f), 170f, 1e-4f);
            Near("归一化: 180 -> 180", IkChain2D.NormalizeDeg(180f), 180f, 1e-4f);
            Near("归一化: 540 -> 180", IkChain2D.NormalizeDeg(540f), 180f, 1e-4f);

            // 相对角跨 ±180 归一化：段 0 = 170°，段 1 = -170° -> 相对 +20°
            c.Build(2, 1f, Vec2.Zero);
            c.Points[0] = new Vec2(0f, 0f);
            c.Points[1] = new Vec2((float)Math.Cos(170f * Math.PI / 180f), (float)Math.Sin(170f * Math.PI / 180f));
            c.Points[2] = c.Points[1] + new Vec2((float)Math.Cos(-170f * Math.PI / 180f), (float)Math.Sin(-170f * Math.PI / 180f));
            joints = c.JointAnglesDeg();
            Near("归一化: 170 与 -170 相对角 = 20°", joints[1], 20f, 1e-3f);

            // ---------- 角度重建一致性 ----------
            c.Build(4, 1f, Vec2.Zero);
            c.SolveCcd(new Vec2(2f, 2f), 64, 1e-3f);
            seg = c.SegmentAnglesDeg();
            Vec2 rebuilt = c.ForwardFromAngles(seg);
            Check("角度: 按段角重建末端一致", (rebuilt - c.End).Length < 1e-3f);

            // ---------- 迭代上限保护 ----------
            c.Build(6, 1f, Vec2.Zero);
            c.SolveCcd(new Vec2(3f, 2f), 1, 1e-3f);
            Check("CCD: 仅 1 次迭代仍保持骨长", LengthsOk(c, 1e-3f));
            Check("CCD: 仅 1 次迭代无 NaN", AllFinite(c.Points));

            c.Build(6, 1f, Vec2.Zero);
            c.SolveFabrik(new Vec2(3f, 2f), 1, 1e-3f);
            Check("FABRIK: 仅 1 次迭代仍保持骨长", LengthsOk(c, 1e-3f));

            // ---------- 重复求解稳定性 ----------
            c.Build(4, 1f, Vec2.Zero);
            c.SolveCcd(new Vec2(2f, 1.5f), 64, 1e-3f);
            Vec2 e1 = c.End;
            c.SolveCcd(new Vec2(2f, 1.5f), 64, 1e-3f);
            Check("CCD: 重复求解稳定", (c.End - e1).Length < 1e-2f);

            // ---------- 批量随机收敛 ----------
            int ccdOk = 0, fabOk = 0;
            Random rnd2 = new Random(777);
            for (int i = 0; i < 100; i++)
            {
                float ang = (float)(rnd2.NextDouble() * Math.PI * 2);
                float rad = (float)(rnd2.NextDouble() * 3.2f + 0.1f);
                Vec2 t = new Vec2((float)Math.Cos(ang) * rad, (float)Math.Sin(ang) * rad);
                IkChain2D a = new IkChain2D(); a.Build(4, 1f, Vec2.Zero);
                IkChain2D b = new IkChain2D(); b.Build(4, 1f, Vec2.Zero);
                if (a.SolveCcd(t, 64, 1e-2f)) ccdOk++;
                if (b.SolveFabrik(t, 64, 1e-2f)) fabOk++;
            }
            Check("CCD: 100 随机目标全部收敛", ccdOk == 100);
            Check("FABRIK: 100 随机目标全部收敛", fabOk == 100);

            // ---------- 极端输入 ----------
            c.Build(3, 1f, Vec2.Zero);
            c.SolveCcd(new Vec2(1e6f, 1e6f), 64, 1e-3f);
            Check("极端目标: 无 NaN", AllFinite(c.Points));
            Near("极端目标: 末端长度=总长", c.End.Length, 3f, 1e-3f);

            c.Build(2, 1f, new Vec2(3f, 4f));
            ok = c.SolveFabrik(new Vec2(5f, 5f), 32, 1e-3f);
            Check("自定义根: 求解后根固定", (c.Points[0] - c.Root).Length < 1e-6f);

            // ---------- 输出 ----------
            Console.WriteLine();
            Console.WriteLine(fail == 0
                ? "RESULT: ALL PASS  (PASS " + pass + ", FAIL 0)"
                : "RESULT: FAILED  (PASS " + pass + ", FAIL " + fail + ")");
            Environment.ExitCode = fail == 0 ? 0 : 1;
        }
    }
}
