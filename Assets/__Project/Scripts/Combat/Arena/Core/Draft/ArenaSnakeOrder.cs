namespace Combat.Arena.Core
{
    /// <summary>
    /// The snake draft order (…P1..Pn, Pn..P1…): maps a zero-based pick index onto a seat index
    /// so pick advantage alternates fairly across rounds (P4-5 req 8).
    /// </summary>
    public static class ArenaSnakeOrder
    {
        public static int SeatIndexAt(int pickIndex, int seatCount)
        {
            int round = pickIndex / seatCount;
            int offset = pickIndex % seatCount;
            bool reversed = round % 2 == 1;
            return reversed ? seatCount - 1 - offset : offset;
        }
    }
}
