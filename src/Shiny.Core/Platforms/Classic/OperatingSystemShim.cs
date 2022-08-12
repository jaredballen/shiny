﻿namespace System;

public static class OperatingSystemShim
{
    public static bool IsAndroidVersionAtLeast(int apiLevel)
    {
#if NET6
        //return OperatingSystem.
        return false;
#elif MONOANDROID
        return (int)Android.OS.Build.VERSION.SdkInt >= apiLevel;
#else
        return false;
#endif
    }

    public static bool IsIOSVersionAtLeast(int osMajor, int osMinor = 0)
    {
#if XAMARINIOS
        return UIKit.UIDevice.CurrentDevice.CheckSystemVersion(osMajor, osMinor);
#else
        return false;
#endif
    }


    public static bool IsMacCatalystVersionAtLeast(int osMajor, int osMinor = 0) => false;
}