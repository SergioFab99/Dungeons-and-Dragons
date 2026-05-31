using System;
using UnityEngine;

namespace DungeonRpg
{
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int X;
        public int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int ManhattanDistance(GridPosition other)
        {
            return Mathf.Abs(X - other.X) + Mathf.Abs(Y - other.Y);
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static GridPosition operator +(GridPosition left, GridPosition right)
        {
            return new GridPosition(left.X + right.X, left.Y + right.Y);
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }

        public static readonly GridPosition Up = new GridPosition(0, 1);
        public static readonly GridPosition Down = new GridPosition(0, -1);
        public static readonly GridPosition Left = new GridPosition(-1, 0);
        public static readonly GridPosition Right = new GridPosition(1, 0);

        public static readonly GridPosition[] CardinalDirections =
        {
            Up,
            Down,
            Left,
            Right
        };
    }
}
