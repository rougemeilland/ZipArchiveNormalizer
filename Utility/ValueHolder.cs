namespace Utility
{
    public class ValueHolder<VALUE_T>
        where VALUE_T : struct
    {
        public ValueHolder()
        {
            Value = default(VALUE_T);
        }

        public VALUE_T Value { get; set; }
    }
}
