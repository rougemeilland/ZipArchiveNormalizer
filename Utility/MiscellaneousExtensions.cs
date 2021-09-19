namespace Utility
{
    public static class MiscellaneousExtensions
    {
        public static bool SignEquals(this int value, int other)
        {
            if (value > 0)
                return other > 0;
            else if (value < 0)
                return other < 0;
            else
                return other == 0;
        }
    }
}
