// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for init-only properties support in .NET Framework 4.8.
/// This type is required by the compiler for init properties.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
