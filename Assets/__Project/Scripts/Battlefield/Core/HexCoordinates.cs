namespace Battlefield
{
    public struct HexCoordinates : IHexCoordinates
    {
        public int Q { get; }
        public int R { get; }
        
        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }
        
        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.Q + b.Q, a.R + b.R);
        }
        
        public static HexCoordinates operator -(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.Q - b.Q, a.R - b.R);
        }
        
        public override bool Equals(object obj)
        {
            if (obj is HexCoordinates other)
            {
                return Q == other.Q && R == other.R;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return Q.GetHashCode() ^ R.GetHashCode();
        }
    }
}

