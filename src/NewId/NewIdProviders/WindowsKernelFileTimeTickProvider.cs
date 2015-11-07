namespace MassTransit.NewIdProviders
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;

    public sealed class WindowsKernelFileTimeTickProvider : ITickProvider
    {
        public long Ticks
        {
            get { return HighResolutionDateTime.UtcNow.Ticks; }
        }
    }

    [SuppressUnmanagedCodeSecurity]
    static class HighResolutionDateTime
    {
        abstract class DateTimeProvider
        {
            public abstract DateTime UtcNow { get; }

            public abstract long FileTimeUtcNow { get; }
        }

        [SuppressUnmanagedCodeSecurity]
        sealed class NativeDateTime : DateTimeProvider
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
            internal static extern void GetSystemTimePreciseAsFileTime(out long filetime);

            static readonly Lazy<bool> IsApiAvailable = new Lazy<bool>(
                () =>
                {
                    try
                    {
                        long filetime;
                        GetSystemTimePreciseAsFileTime(out filetime);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });

            public static bool IsAvailable
            {
                get { return IsApiAvailable.Value; }
            }

            public override long FileTimeUtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [SuppressUnmanagedCodeSecurity]
                get
                {
                    long time;
                    GetSystemTimePreciseAsFileTime(out time);
                    return time;
                }
            }

            public override DateTime UtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [SuppressUnmanagedCodeSecurity]
                get
                {
                    long time;
                    GetSystemTimePreciseAsFileTime(out time);
                    return DateTime.FromFileTimeUtc(time);
                }
            }
        }

        sealed class ManagedDateTime : DateTimeProvider
        {
            public override DateTime UtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return DateTime.UtcNow; }
            }

            public override long FileTimeUtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return DateTime.UtcNow.ToFileTimeUtc(); }
            }
        }

        static readonly DateTimeProvider Provider;
        static readonly bool _isHighRes;

        public static bool IsHighPrecision
        {
            get { return _isHighRes; }
        }

        public static long FileTimeUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [SuppressUnmanagedCodeSecurity]
            get
            {
                return Provider.FileTimeUtcNow;
            }
        }

        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [SuppressUnmanagedCodeSecurity]
            get
            {
                if (_isHighRes)
                {
                    long time;
                    NativeDateTime.GetSystemTimePreciseAsFileTime(out time);
                    return DateTime.FromFileTimeUtc(time);                    
                }
                return Provider.UtcNow;
            }
        }

        static HighResolutionDateTime()
        {
            Provider = NativeDateTime.IsAvailable ? (DateTimeProvider)new NativeDateTime() : new ManagedDateTime();
            _isHighRes = Provider is NativeDateTime;
        }
    }
}