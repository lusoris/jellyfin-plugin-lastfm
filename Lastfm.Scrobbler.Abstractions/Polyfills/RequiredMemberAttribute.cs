// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for required members support in .NET Framework 4.8.
/// This attribute is used to mark required properties.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class RequiredMemberAttribute : Attribute
{
}

/// <summary>
/// Polyfill for compiler feature required attribute support in .NET Framework 4.8.
/// This attribute is used to indicate that a type requires compiler features.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class CompilerFeatureRequiredAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilerFeatureRequiredAttribute"/> class.
    /// </summary>
    /// <param name="featureName">The name of the required compiler feature.</param>
    public CompilerFeatureRequiredAttribute(string featureName)
    {
        FeatureName = featureName;
    }

    /// <summary>
    /// Gets the name of the compiler feature.
    /// </summary>
    public string FeatureName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the feature is optional.
    /// </summary>
    public bool IsOptional { get; init; }
}
