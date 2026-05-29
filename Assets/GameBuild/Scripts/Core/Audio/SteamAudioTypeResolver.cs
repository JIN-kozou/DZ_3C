using System;

/// <summary>
/// Resolves Steam Audio runtime types without a hard assembly reference to SteamAudioUnity.
/// </summary>
public static class SteamAudioTypeResolver
{
    private const string AssemblyName = "SteamAudioUnity";

    public static Type Manager => Resolve("SteamAudioManager");
    public static Type Geometry => Resolve("SteamAudioGeometry");

    public static bool IsInstalled => Manager != null;

    private static Type Resolve(string typeName)
    {
        return Type.GetType($"SteamAudio.{typeName}, {AssemblyName}");
    }
}
