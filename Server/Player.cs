namespace Simulation
{
    // long due to floating point determinism stuff - would be fixed point probably
    // Stand-in struct for all game entities.
    public struct Player
    {
        public long X;
        public long Y;
        public long VelocityX;
        public long VelocityY;
        public bool Moving;

        public override readonly string ToString()
        {
            return $"Player: P = {X}/{Y}, V = {VelocityX}/{VelocityY}, M = {Moving}";
        }



        public static Player Interpolate(Player a, Player b, float alpha)
        {
            return new Player
            {
                X = Lerp(a.X, b.X, alpha),
                Y = Lerp(a.Y, b.Y, alpha),
                VelocityX = Lerp(a.VelocityX, a.VelocityX, alpha),
                VelocityY = Lerp(a.VelocityY, a.VelocityY, alpha),
                // Can't interpolate bool, just take last state.
                Moving = b.Moving,
            };
        }

        static long Lerp(long a, long b, float alpha)
        {
            var result = (long)((float)a * alpha + (float)b * (1 - alpha));
            return result;
        }

        // Equality checks to allow us to easily check for differences
        public override readonly bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Player)obj;
            return X == other.X && Y == other.Y && VelocityX == other.VelocityX && VelocityY == other.VelocityY && Moving == other.Moving;
        }

        public static bool operator ==(Player left, Player right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Player left, Player right)
        {
            return !(left == right);
        }
    }


}