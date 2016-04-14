using System;
using UnityEngine;
using Object = System.Object;
using Random = System.Random;

[Serializable]
public struct Vector2D
{
    #region static variables
    private static readonly Random rnd = new Random();
    private const float scaleV3 = 10;
    #endregion

    #region variables

    public double X;
    public double Y;

    public int IntX { get { return (int)X; } }
    public int IntY { get { return (int)Y; } }

    public double Quadratic { get { return X * X + Y * Y; } }

    // Returns the modulus ('length') of the vector
    public double Modulus()
    {
        return Math.Sqrt(Dot(this));
    }

    // Returns the scalar product of the vector with the argument
    private double Dot(Vector2D vector)
    {
        return X * vector.X + Y * vector.Y;
    }

    // Returns a new vector created by normalizing the receiver
    public Vector2D Unit()
    {
        double r = Modulus();
        return 0 == r ? new Vector2D() : new Vector2D(X / r, Y / r);
    }

    public Vector3 Vector3
    {
        get
        {
            return new Vector3((float)X / scaleV3, 0f,(float)Y/ scaleV3);
        }
    }
    #endregion

    #region constructor

    public Vector2D(Vector3 unityVector)
    {
        X = unityVector.x* scaleV3;
        Y = unityVector.z* scaleV3;
    }

    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static Vector2D GetRandom(int disperse)
    {
        return new Vector2D(rnd.Next(-disperse, disperse + 1), rnd.Next(-disperse, disperse + 1));
    }
    #endregion

    #region operators

    public override bool Equals(Object obj)
    {
        if (obj is Vector2D)
        {
            Vector2D vector = (Vector2D)obj;
            return (X == vector.X) && (Y == vector.Y);
        }

        return false;
    }

    // Override the ToString() method to display a complex number in the traditional format:
    public override string ToString()
    {
        return (String.Format("{0},{1}", IntX, IntY));
    }

    public static bool operator ==(Vector2D v1, Vector2D v2)
    {
        return (v1.X == v2.X) && (v1.Y == v2.Y);
    }

    public static bool operator !=(Vector2D v1, Vector2D v2)
    {
        return (v1.X != v2.X) || (v1.Y != v2.Y);
    }

    public static Vector2D operator +(Vector2D v1, Vector2D v2)
    {
        return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
    }

    public static Vector2D operator -(Vector2D v1, Vector2D v2)
    {
        return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
    }

    public static Vector2D operator *(Vector2D v, double x)
    {
        return new Vector2D(x * v.X, x * v.Y);
    }

    public static Vector2D operator -(Vector2D v)
    {
        return new Vector2D(-v.X, -v.Y);
    }

    #endregion

    #region functions

    /// <summary>Returns the angle between the vector and the argument (also a vector)</summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public double GetAngle(Vector2D vector)
    {
        double dotSin = X * vector.Y - Y * vector.X;
        double dotCos = X * vector.X + Y * vector.Y;

        double mod1 = Dot(this);
        double mod2 = vector.Dot(vector);

        if (0 == mod1 * mod2) return 0;
        mod1 = Math.Sqrt(mod1 * mod2);

        dotSin = dotSin / (mod1);
        dotCos = dotCos / (mod1);

        if (dotCos >= 0) return Math.Asin(dotSin);

        double result = (dotSin >= 0) ? (Math.PI - Math.Asin(dotSin)) : (-Math.PI - Math.Asin(dotSin));

        return result;
    }

    public double GetDistance(Vector2D vector)
    {
        Vector2D path = vector - this;
        return path.Modulus();
    }

    public double GetDistance2(Vector2D vector)
    {
        Vector2D path = vector - this;
        return path.Quadratic;
    }

    public bool IsParallelTo(Vector2D vector)
    {
        double dotCos = X * vector.Y + Y * vector.X;
        dotCos = dotCos * dotCos;
        double mod = Dot(this) * vector.Dot(vector);

        return dotCos == mod;
    }

    // Returns true if the vector is antiparallel to the argument
    public bool IsPerpendicularTo(Vector2D vector)
    {
        double dotCos = X * vector.Y + Y * vector.X;

        return 0 == dotCos;
    }

    public Vector2D Rotate(double alfa)
    {
        double sin = Math.Sin(alfa);
        double cos = Math.Cos(alfa);

        return new Vector2D(
            X * cos - Y * sin,

            X * sin + Y * cos);
    }

    public Vector2D Rotate(double alfa, Vector2D dotRotate)
    {
        Vector2D vector = this - dotRotate;
        vector = vector.Rotate(alfa) + dotRotate;

        return vector;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    #endregion
}