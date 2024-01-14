namespace Simulation;

    // long due to floating point determinism stuff - would be fixed point probably
    // Stand-in struct for all game entities.
    public struct Player
    {
        public long X;
        public long Y;
        public long TargetX;
        public long TargetY;
        public long VelocityX;
        public long VelocityY;
        public bool Moving;


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

        public Player Update()
        {
            if (this.Moving)
            {
                if (FixedPointUtil.Distance(X, Y, TargetX, TargetY) < FixedPointUtil.One * 2) {
                    return new Player
                    {
                        X = this.TargetX,
                        Y = this.TargetY,
                        VelocityX = this.VelocityX,
                        VelocityY = this.VelocityY,
                        TargetX = this.TargetX,
                        TargetY = this.TargetY,
                        // Stop moving
                        Moving = false,
                    };
                }

                return new Player
                {
                    X = this.X + this.VelocityX,
                    Y = this.Y + this.VelocityY,
                    VelocityX = this.VelocityX,
                    VelocityY = this.VelocityY,
                    TargetX = this.TargetX,
                    TargetY = this.TargetY,
                    Moving = this.Moving,
                };
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

            var other = (Player)obj;
            return X == other.X && Y == other.Y && VelocityX == other.VelocityX && VelocityY == other.VelocityY && Moving == other.Moving
                && TargetX == other.TargetX && TargetY == other.TargetY;
        }

        public static bool operator ==(Player left, Player right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Player left, Player right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return $"Player: P = {X}/{Y}, V = {VelocityX}/{VelocityY}, M = {Moving}, T = {TargetX}/{TargetY}";
        }
    }

