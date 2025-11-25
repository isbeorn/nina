#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Media.Media3D
{
    // Alias OpenCV types for 3D points and vectors
    public class Point3D
    {
        private OpenCvSharp.Point3d _point;

        public double X
        {
            get => _point.X;
            set => _point.X = value;
        }

        public double Y
        {
            get => _point.Y;
            set => _point.Y = value;
        }

        public double Z
        {
            get => _point.Z;
            set => _point.Z = value;
        }

        public Point3D() : this(0, 0, 0) { }

        public Point3D(double x, double y, double z)
        {
            _point = new OpenCvSharp.Point3d(x, y, z);
        }

        public static implicit operator OpenCvSharp.Point3d(Point3D p) =>
            new OpenCvSharp.Point3d(p.X, p.Y, p.Z);

        public static implicit operator Point3D(OpenCvSharp.Point3d p) =>
            new Point3D(p.X, p.Y, p.Z);
    }

    public class Vector3D
    {
        private OpenCvSharp.Vec3d _vec;

        public double X
        {
            get => _vec.Item0;
            set => _vec.Item0 = value;
        }

        public double Y
        {
            get => _vec.Item1;
            set => _vec.Item1 = value;
        }

        public double Z
        {
            get => _vec.Item2;
            set => _vec.Item2 = value;
        }

        public Vector3D() : this(0, 0, 0) { }

        public Vector3D(double x, double y, double z)
        {
            _vec = new OpenCvSharp.Vec3d(x, y, z);
        }

        public double Length => OpenCvSharp.Cv2.Norm(_vec);

        public void Normalize()
        {
            double length = Length;
            if (length > 0)
            {
                _vec = _vec / length;
            }
        }

        public static Vector3D operator +(Vector3D v1, Vector3D v2) =>
            new Vector3D { _vec = v1._vec + v2._vec };

        public static Vector3D operator -(Vector3D v1, Vector3D v2) =>
            new Vector3D { _vec = v1._vec - v2._vec };

        public static Vector3D operator *(Vector3D v, double scalar) =>
            new Vector3D { _vec = v._vec * scalar };

        public static Vector3D operator *(double scalar, Vector3D v) => v * scalar;

        public static double operator *(Vector3D v1, Vector3D v2) =>
            v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

        public static Vector3D CrossProduct(Vector3D v1, Vector3D v2) =>
            new Vector3D(
                v1.Y * v2.Z - v1.Z * v2.Y,
                v1.Z * v2.X - v1.X * v2.Z,
                v1.X * v2.Y - v1.Y * v2.X
            );

        public static double DotProduct(Vector3D v1, Vector3D v2) => v1 * v2;

        public static implicit operator OpenCvSharp.Vec3d(Vector3D v) =>
            new OpenCvSharp.Vec3d(v.X, v.Y, v.Z);

        public static implicit operator Vector3D(OpenCvSharp.Vec3d v) =>
            new Vector3D(v.Item0, v.Item1, v.Item2);
    }
}
