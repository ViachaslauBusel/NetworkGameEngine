using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class MatrixTest
    {
        // Тестирование матрицы на скорсть преобразования двух мерного вектора
        [Test]
        public void MatrixTest_0()
        {
            HashSet<int> hashSet = new HashSet<int>();

            Assert.IsTrue(hashSet.Add(GenerateHash(0, 0)));
            Assert.IsTrue(hashSet.Add(GenerateHash(0, 1)));
            Assert.IsTrue(hashSet.Add(GenerateHash(0, -1)));
            Assert.IsTrue(hashSet.Add(GenerateHash(1, 0)));
            Assert.IsTrue(hashSet.Add(GenerateHash(-1, 0)));
            Assert.IsTrue(hashSet.Add(GenerateHash(1, 1)));
            Assert.IsTrue(hashSet.Add(GenerateHash(-1, -1)));
            Assert.IsTrue(hashSet.Add(GenerateHash(1, -1)));
            Assert.IsTrue(hashSet.Add(GenerateHash(-1, 1)));
        }

        [Test]
        public void TestAngle()
        {
            Vector2 vector1 = new Vector2(0, 1);
            Vector2 vector2 = new Vector2(-1, 0);

            float angle = AngleBetween(Vector2.Normalize(vector1), Vector2.Normalize(vector2));

            Vector2 test_0 = Rotate(new Vector2(0, -1), angle);
            Vector2 test_1 = Rotate(new Vector2(1, 0), angle);
            Vector2 test_2 = Rotate(new Vector2(0, 1), angle);
            Vector2 test_3 = Rotate(new Vector2(-1, 0), angle);


        }

        public static float AngleBetween(Vector2 vector1, Vector2 vector2)
        {
            float angle = MathF.Atan2(vector2.Y, vector2.X) - MathF.Atan2(vector1.Y, vector1.X);
            return angle < 0 ? -angle : 2 * MathF.PI - angle;
        }

        public Vector2 Rotate(Vector2 vector, float angleInRadians)
        {
            float cosTheta = MathF.Cos(angleInRadians);
            float sinTheta = MathF.Sin(angleInRadians);
            return new Vector2(
                vector.X * cosTheta + vector.Y * sinTheta,
               -vector.X * sinTheta + vector.Y * cosTheta
            );
        }

        public int GenerateHash(int x, int y)
        {
            return x ^ y << 16;
        }
    }
}
