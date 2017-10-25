using System.Collections.Generic;

/// <summary>
/// The models that includes all data needed to track the progression of the player.
/// Can be serialized and stored.
/// </summary>
public class PlayerProgressionData
{
    /// <summary>
    /// Holds the names of all completed levels.
    /// Needed to define which levels are unlocked and if levels should be displayed as completed or not.
    /// </summary>
    public List<string> CompletedLevelNames { get; set; }

    /// <summary>
    /// Holds the name of the level that was played last. 
    /// Needed to position the level selection unit at that level everytime the user enters the level selection.
    /// </summary>
    public string LastPlayedLevelName { get; set; }

    /// <summary>
    /// Holds the name of the last unlocked level.
    /// Needed to display an unlocked animation for newly unlocked level.
    /// </summary>
    public string LastUnlockedLevelName { get; set; }
}
