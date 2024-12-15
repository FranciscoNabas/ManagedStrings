// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;

namespace ManagedStrings.Engine;

// Many of the unmanaged solutions in this program were inspired (*ahem* copied)
// From the great SystemInformer.
// Some of the native APIs and structures are version dependent.
// There they use a series of macros and a global variable holding the current
// Windows version. Here we implement a class to hold this information so we can
// easily compare throughout the code.

/// <summary>
/// Holds information about the current Windows version.
/// </summary>
internal static class WinVer
{
    internal const uint WINDOWS_ANCIENT      = 0;
    internal const uint WINDOWS_XP           = 51;
    internal const uint WINDOWS_SERVER_2003  = 52;
    internal const uint WINDOWS_VISTA        = 60;
    internal const uint WINDOWS_7            = 61;
    internal const uint WINDOWS_8            = 62;
    internal const uint WINDOWS_8_1          = 63;
    internal const uint WINDOWS_10           = 100;
    internal const uint WINDOWS_10_TH2       = 101;
    internal const uint WINDOWS_10_RS1       = 102;
    internal const uint WINDOWS_10_RS2       = 103;
    internal const uint WINDOWS_10_RS3       = 104;
    internal const uint WINDOWS_10_RS4       = 105;
    internal const uint WINDOWS_10_RS5       = 106;
    internal const uint WINDOWS_10_19H1      = 107;
    internal const uint WINDOWS_10_19H2      = 108;
    internal const uint WINDOWS_10_20H1      = 109;
    internal const uint WINDOWS_10_20H2      = 110;
    internal const uint WINDOWS_10_21H1      = 111;
    internal const uint WINDOWS_10_21H2      = 112;
    internal const uint WINDOWS_10_22H2      = 113;
    internal const uint WINDOWS_11           = 114;
    internal const uint WINDOWS_11_22H2      = 115;
    internal const uint WINDOWS_11_23H2      = 116;
    internal const uint WINDOWS_11_24H2      = 117;
    internal const uint WINDOWS_NEW          = uint.MaxValue;

    internal static uint CurrentVersion { get; }

    static WinVer()
    {
        Version version = Environment.OSVersion.Version;
        if (version.Major == 6 && version.Minor < 1 || version.Major < 6) {
            CurrentVersion = WINDOWS_ANCIENT;
        }
        // Windows 7, Windows Server 2008 R2
        else if (version.Major == 6 && version.Minor == 1) {
            CurrentVersion = WINDOWS_7;
        }
        // Windows 8, Windows Server 2012
        else if (version.Major == 6 && version.Minor == 2) {
            CurrentVersion = WINDOWS_8;
        }
        // Windows 8.1, Windows Server 2012 R2
        else if (version.Major == 6 && version.Minor == 3) {
            CurrentVersion = WINDOWS_8_1;
        }
        // Windows 10, Windows Server 2016
        else if (version.Major == 10 && version.Minor == 0) {
            if (version.Build > 26000) {
                CurrentVersion = WINDOWS_NEW;
            }
            else if (version.Build >= 26000) {
                CurrentVersion = WINDOWS_11_24H2;
            }
            else if (version.Build >= 22631) {
                CurrentVersion = WINDOWS_11_23H2;
            }
            else if (version.Build >= 22621) {
                CurrentVersion = WINDOWS_11_22H2;
            }
            else if (version.Build >= 22000) {
                CurrentVersion = WINDOWS_11;
            }
            else if (version.Build >= 19045) {
                CurrentVersion = WINDOWS_10_22H2;
            }
            else if (version.Build >= 19044) {
                CurrentVersion = WINDOWS_10_21H2;
            }
            else if (version.Build >= 19043) {
                CurrentVersion = WINDOWS_10_21H1;
            }
            else if (version.Build >= 19042) {
                CurrentVersion = WINDOWS_10_20H2;
            }
            else if (version.Build >= 19041) {
                CurrentVersion = WINDOWS_10_20H1;
            }
            else if (version.Build >= 18363) {
                CurrentVersion = WINDOWS_10_19H2;
            }
            else if (version.Build >= 18362) {
                CurrentVersion = WINDOWS_10_19H1;
            }
            else if (version.Build >= 17763) {
                CurrentVersion = WINDOWS_10_RS5;
            }
            else if (version.Build >= 17134) {
                CurrentVersion = WINDOWS_10_RS4;
            }
            else if (version.Build >= 16299) {
                CurrentVersion = WINDOWS_10_RS3;
            }
            else if (version.Build >= 15063) {
                CurrentVersion = WINDOWS_10_RS2;
            }
            else if (version.Build >= 14393) {
                CurrentVersion = WINDOWS_10_RS1;
            }
            else if (version.Build >= 10586) {
                CurrentVersion = WINDOWS_10_TH2;
            }
            else if (version.Build >= 10240) {
                CurrentVersion = WINDOWS_10;
            }
            else {
                CurrentVersion = WINDOWS_10;
            }
        }
        else {
            CurrentVersion = WINDOWS_NEW;
        }
    }
}