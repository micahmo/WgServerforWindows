using System;

namespace WgServerforWindows
{
    public class TimeCachedProperty<TValue>
    {
        public TimeCachedProperty(TimeSpan cacheTime, Func<TValue> calculateValueFunc)
        {
            _cacheTime = cacheTime;
            _calculateValueFunc = calculateValueFunc ?? throw new ArgumentNullException(nameof(calculateValueFunc));
        }

        public static implicit operator TValue(TimeCachedProperty<TValue> timeCachedProperty) => timeCachedProperty.Value;

        public TValue Value
        {
            get
            {
                if (_lastAccessedTime + _cacheTime < DateTimeOffset.Now || _cachedValue is null)
                {
                    _cachedValue = _calculateValueFunc();
                }

                _lastAccessedTime = DateTimeOffset.Now;

                return _cachedValue;
            }
        }
        private TValue _cachedValue;

        private readonly Func<TValue> _calculateValueFunc;
        private readonly TimeSpan _cacheTime;
        private DateTimeOffset _lastAccessedTime;
    }

    // Implement some custom operators on Boolean type TimeCachedProperties
    // so that this type works well with CalcBinding and Expressions (which can't take advantage of the implicit constructor)
    public class BooleanTimeCachedProperty : TimeCachedProperty<bool>
    {
        public BooleanTimeCachedProperty(TimeSpan cacheTime, Func<bool> calculateValueFunc) 
            : base(cacheTime, calculateValueFunc) { }

        public static bool operator !(BooleanTimeCachedProperty @this) => !@this.Value;

        public static bool operator true(BooleanTimeCachedProperty @this) => @this.Value;

        public static bool operator false(BooleanTimeCachedProperty @this) => !@this.Value;

        public static BooleanTimeCachedProperty operator &(BooleanTimeCachedProperty @this, BooleanTimeCachedProperty other) =>
            @this.Value && other.Value
                ? new BooleanTimeCachedProperty(TimeSpan.Zero, () => true)
                : new BooleanTimeCachedProperty(TimeSpan.Zero, () => false);

        public static BooleanTimeCachedProperty operator |(BooleanTimeCachedProperty @this, BooleanTimeCachedProperty other) =>
            @this.Value || other.Value
                ? new BooleanTimeCachedProperty(TimeSpan.Zero, () => true)
                : new BooleanTimeCachedProperty(TimeSpan.Zero, () => false);
    }
}
