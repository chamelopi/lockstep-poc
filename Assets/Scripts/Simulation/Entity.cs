#nullable enable

using System;

namespace Simulation
{
    // long due to floating point determinism stuff - would be fixed point probably
    public struct Entity
    {
        public long X;
        public long Y;
        public long TargetX;
        public long TargetY;
        public long VelocityX;
        public long VelocityY;
        public bool Moving;
        public int OwningPlayer;
        public int EntityId;


        public static Entity Interpolate(Entity a, Entity b, float alpha)
        {
            return new Entity
            {
                X = Lerp(a.X, b.X, alpha),
                Y = Lerp(a.Y, b.Y, alpha),
                TargetX = Lerp(a.TargetX, b.TargetX, alpha),
                TargetY = Lerp(a.TargetY, b.TargetY, alpha),
                VelocityX = Lerp(a.VelocityX, a.VelocityX, alpha),
                VelocityY = Lerp(a.VelocityY, a.VelocityY, alpha),
                // Can't interpolate bool, just take last state.
                Moving = b.Moving,
                OwningPlayer = b.OwningPlayer,
            };
        }

        static long Lerp(long a, long b, float alpha)
        {
            var result = (long)((float)a * alpha + (float)b * (1 - alpha));
            return result;
        }

        public Entity Update()
        {
            if (this.Moving)
            {
                if (FixedPointUtil.Distance(X, Y, TargetX, TargetY) < FixedPointUtil.One * 2)
                {
                    this.X = TargetX;
                    this.Y = TargetY;
                    this.Moving = false;
                    return this;
                }

                X = this.X + this.VelocityX;
                Y = this.Y + this.VelocityY;
                return this;
            }
            else
            {
                return this;
            }
        }

        // Equality checks to allow us to easily check for differences
        public override readonly bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Entity)obj;
            return X == other.X && Y == other.Y && VelocityX == other.VelocityX && VelocityY == other.VelocityY && Moving == other.Moving
                && TargetX == other.TargetX && TargetY == other.TargetY && OwningPlayer == other.OwningPlayer;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, VelocityX, VelocityY, Moving, TargetX, TargetY, OwningPlayer);
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return $"Entity: P = {X}/{Y}, V = {VelocityX}/{VelocityY}, M = {Moving}, T = {TargetX}/{TargetY}";
        }
    }


}
