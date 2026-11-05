using System;

namespace IkLab
{
    /// <summary>
    /// 二维关节链 IK 求解器（纯数学，不依赖引擎）。
    /// 链由 n 段等长骨构成：Points[0]=根（固定），Points[n]=末端。
    /// 提供两种经典迭代算法：CCD（循环坐标下降）与 FABRIK（前后向迭代）。
    /// </summary>
    public class IkChain2D
    {
        public Vec2 Root = Vec2.Zero;
        public float SegmentLength = 1f;
        public int JointCount;          // 段数 n（关节数 n，另有根与末端端点）
        public Vec2[] Points;           // 长度 n+1

        public float TotalLength
        {
            get { return JointCount * SegmentLength; }
        }

        public Vec2 End
        {
            get { return Points[JointCount]; }
        }

        /// <summary>按关节数、骨长、根位置构建一条沿 +X 伸直的标准链。</summary>
        public void Build(int joints, float length, Vec2 root)
        {
            JointCount = Math.Max(1, joints);
            SegmentLength = Math.Max(1e-4f, length);
            Root = root;
            Points = new Vec2[JointCount + 1];
            for (int i = 0; i <= JointCount; i++)
                Points[i] = new Vec2(root.X + SegmentLength * i, root.Y);
        }

        public float EndDistanceTo(Vec2 target)
        {
            return (End - target).Length;
        }

        /// <summary>把整条链沿目标方向拉直（用于超出可达范围时）。</summary>
        public void StretchToward(Vec2 target)
        {
            Vec2 d = target - Root;
            Vec2 u = d.IsZero ? new Vec2(1f, 0f) : d.Normalized();
            for (int i = 0; i <= JointCount; i++)
                Points[i] = Root + u * (SegmentLength * i);
        }

        // ---------------------------------------------------------------
        // CCD：从末端关节向根部逐个旋转，使末端逐步逼近目标
        // ---------------------------------------------------------------
        public bool SolveCcd(Vec2 target, int maxIterations, float tolerance)
        {
            if (Points == null || Points.Length < 2)
                return false;

            // 目标超出可达范围：直接伸直指向目标
            Vec2 toTarget = target - Root;
            if (toTarget.Length >= TotalLength)
            {
                StretchToward(target);
                return toTarget.Length <= TotalLength + tolerance;
            }

            for (int iter = 0; iter < maxIterations; iter++)
            {
                for (int j = JointCount - 1; j >= 0; j--)
                {
                    Vec2 a = Points[j];
                    Vec2 toEnd = End - a;
                    Vec2 toTar = target - a;
                    if (toEnd.IsZero || toTar.IsZero)
                        continue;

                    float ang = Vec2.SignedAngle(toEnd, toTar);
                    RotateFrom(j + 1, a, ang);
                }

                if (EndDistanceTo(target) <= tolerance)
                    return true;
            }
            return EndDistanceTo(target) <= tolerance;
        }

        private void RotateFrom(int firstIndex, Vec2 center, float radians)
        {
            float c = (float)Math.Cos(radians);
            float s = (float)Math.Sin(radians);
            for (int i = firstIndex; i <= JointCount; i++)
            {
                Vec2 d = Points[i] - center;
                Points[i] = new Vec2(center.X + d.X * c - d.Y * s,
                                     center.Y + d.X * s + d.Y * c);
            }
        }

        // ---------------------------------------------------------------
        // FABRIK：后向（末端贴目标，逐段回推） + 前向（根固定，逐段推回）
        // ---------------------------------------------------------------
        public bool SolveFabrik(Vec2 target, int maxIterations, float tolerance)
        {
            if (Points == null || Points.Length < 2)
                return false;

            float total = TotalLength;
            Vec2 d = target - Root;
            float dist = d.Length;

            // 目标在可达范围之外：伸直指向目标
            if (dist >= total)
            {
                StretchToward(target);
                return dist <= total + tolerance;
            }

            // 目标与根重合：折叠（无明确方向），保持合法构型
            if (dist < 1e-6f)
            {
                Vec2 u = new Vec2(1f, 0f);
                for (int i = 0; i <= JointCount; i++)
                    Points[i] = Root + u * (SegmentLength * i);
                return EndDistanceTo(target) <= tolerance;
            }

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // 后向：末端对齐目标，从末端向根逐段回推
                Points[JointCount] = target;
                for (int i = JointCount - 1; i >= 0; i--)
                {
                    Vec2 dir = Points[i] - Points[i + 1];
                    Vec2 u = dir.IsZero ? FallbackDir(i) : dir.Normalized();
                    Points[i] = Points[i + 1] + u * SegmentLength;
                }

                // 前向：根归位，从根向末端逐段推到正确骨长
                Points[0] = Root;
                for (int i = 1; i <= JointCount; i++)
                {
                    Vec2 dir = Points[i] - Points[i - 1];
                    Vec2 u = dir.IsZero ? FallbackDir(i - 1) : dir.Normalized();
                    Points[i] = Points[i - 1] + u * SegmentLength;
                }

                if (EndDistanceTo(target) <= tolerance)
                    return true;
            }
            return EndDistanceTo(target) <= tolerance;
        }

        private Vec2 FallbackDir(int segmentIndex)
        {
            // 选取相邻段方向作为退化时的替代方向；都没有则用 +X
            if (Points != null && segmentIndex + 1 < Points.Length)
            {
                Vec2 d = Points[segmentIndex + 1] - Points[segmentIndex];
                if (!d.IsZero)
                    return d.Normalized();
            }
            if (Points != null && segmentIndex - 1 >= 0)
            {
                Vec2 d = Points[segmentIndex] - Points[segmentIndex - 1];
                if (!d.IsZero)
                    return d.Normalized();
            }
            return new Vec2(1f, 0f);
        }

        // ---------------------------------------------------------------
        // 角度输出
        // ---------------------------------------------------------------
        /// <summary>各段绝对方向角（度，相对 +X 轴，逆时针为正）。</summary>
        public float[] SegmentAnglesDeg()
        {
            float[] angles = new float[JointCount];
            for (int i = 0; i < JointCount; i++)
            {
                Vec2 d = Points[i + 1] - Points[i];
                angles[i] = (float)(Math.Atan2(d.Y, d.X) * 180.0 / Math.PI);
            }
            return angles;
        }

        /// <summary>
        /// 各关节角度（度）：关节 0 为根关节（整链朝向，相对 +X 的绝对角）；
        /// 关节 i (i≥1) 为相对上一段的转角，归一化到 (-180, 180]。
        /// </summary>
        public float[] JointAnglesDeg()
        {
            float[] seg = SegmentAnglesDeg();
            float[] joints = new float[JointCount];
            if (JointCount <= 0)
                return joints;
            joints[0] = NormalizeDeg(seg[0]);
            for (int i = 1; i < JointCount; i++)
                joints[i] = NormalizeDeg(seg[i] - seg[i - 1]);
            return joints;
        }

        public static float NormalizeDeg(float deg)
        {
            float a = deg % 360f;
            if (a > 180f)
                a -= 360f;
            if (a <= -180f)
                a += 360f;
            return a;
        }

        /// <summary>按段角重建末端位置（用于校验角度一致性）。</summary>
        public Vec2 ForwardFromAngles(float[] segmentAnglesDeg)
        {
            Vec2 p = Root;
            for (int i = 0; i < JointCount; i++)
            {
                float rad = segmentAnglesDeg[i] * (float)Math.PI / 180f;
                p = p + Vec2.FromAngle(rad) * SegmentLength;
            }
            return p;
        }
    }
}
