using System;

namespace IkLab
{
    /// <summary>轻量二维向量（与 UnityEngine.Vector2 字段对齐）。</summary>
    public struct Vec2
    {
        public float X, Y;

        public Vec2(float x, float y)
        {
            X = x; Y = y;
        }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float LengthSquared
        {
            get { return X * X + Y * Y; }
        }

        public float Length
        {
            get { return (float)Math.Sqrt(LengthSquared); }
        }

        public bool IsZero
        {
            get { return LengthSquared < 1e-10f; }
        }

        public Vec2 Normalized()
        {
            float l = Length;
            if (l < 1e-8f)
                return Zero;
            return new Vec2(X / l, Y / l);
        }

        /// <summary>绕原点旋转（弧度，逆时针为正）。</summary>
        public Vec2 Rotate(float radians)
        {
            float c = (float)Math.Cos(radians);
            float s = (float)Math.Sin(radians);
            return new Vec2(X * c - Y * s, X * s + Y * c);
        }

        /// <summary>由弧度角构造单位向量。</summary>
        public static Vec2 FromAngle(float radians)
        {
            return new Vec2((float)Math.Cos(radians), (float)Math.Sin(radians));
        }

        public static float Dot(Vec2 a, Vec2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>二维叉积（标量）：a × b = ax·by − ay·bx。</summary>
        public static float Cross(Vec2 a, Vec2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>a 到 b 的带符号夹角（弧度，[-π, π]）。</summary>
        public static float SignedAngle(Vec2 a, Vec2 b)
        {
            return (float)Math.Atan2(Cross(a, b), Dot(a, b));
        }

        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X + b.X, a.Y + b.Y);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X - b.X, a.Y - b.Y);
        }

        public static Vec2 operator *(Vec2 a, float s)
        {
            return new Vec2(a.X * s, a.Y * s);
        }

        public static Vec2 operator *(float s, Vec2 a)
        {
            return new Vec2(a.X * s, a.Y * s);
        }

        public override string ToString()
        {
            return "(" + X.ToString("F3") + ", " + Y.ToString("F3") + ")";
        }
    }
}
