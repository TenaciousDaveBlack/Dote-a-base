using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using static FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.DynamicEvent.Delegates;

namespace DoteTracker;

public enum DoteState
{
    None,
    DotedThem,    // Local player typed /dote on this person
    TheyDotedMe,  // This person typed /dote on the local player
    Mutual        // Both occurred
}

/// <summary>
/// Holds all tracked dote interactions for the current session and parses
/// incoming chat messages to keep the roster up to date.
/// </summary>
public sealed class DoteTrackerState
{
    // Key: normalised player name (lower-invariant comparison via StringComparer)
    public readonly Dictionary<string, DoteState> DoteRoster =
        new(StringComparer.OrdinalIgnoreCase);

    // "You dote on John Doe."
    private static readonly Regex OutgoingRegex =
        new(@"You dote on (?<targetName>[A-Za-z'\-\s]+)\.", RegexOptions.Compiled);

    // "Jane Doe dotes on you."
    private static readonly Regex IncomingRegex =
        new(@"(?<sourceName>[A-Za-z'\-\s]+) dotes on you\.", RegexOptions.Compiled);

    // Cross-world icon that sometimes appears in player names from the chat log
    private const char CrossWorldIcon = '\uE05D';

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Strips the cross-world icon, server suffix (e.g. "@Excalibur"), and
    /// surrounding whitespace from a raw name string so names from the chat log
    /// and the ObjectTable compare correctly.
    /// </summary>
    public static string NormalizeName(string name)
    {
        name = name.Replace(CrossWorldIcon.ToString(), string.Empty);
        var atIndex = name.IndexOf('@');
        if (atIndex >= 0)
            name = name[..atIndex];
        return name.Trim();
    }

    /// <summary>Wipes all tracked interactions.</summary>
    public void Clear() => DoteRoster.Clear();

    // -------------------------------------------------------------------------
    // Internals
    // -------------------------------------------------------------------------

    private void UpdateState(string name, bool isOutgoing)
    {
        DoteRoster.TryGetValue(name, out var current);

        DoteRoster[name] = isOutgoing
            ? current == DoteState.TheyDotedMe ? DoteState.Mutual : DoteState.DotedThem
            : current == DoteState.DotedThem   ? DoteState.Mutual : DoteState.TheyDotedMe;
    }
}
