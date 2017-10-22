using System.Collections.Generic;

/// <summary>
/// Handles storing, updating and accessability of the <see cref="PlayerProgressionData"/>.
/// </summary>
public class PlayerProgressionService
{
    private readonly ISerializer m_serializer;
    private readonly IStorageHelper m_storageHelper;
    private readonly PlayerProgressionData m_latestPlayerProgressionData;

    /// <summary>
    /// Gets or sets the name of the last played level.
    /// </summary>
    public string LastPlayedLevel
    {
        get
        {
            return m_latestPlayerProgressionData.LastPlayedLevelName;
        }

        set
        {
            m_latestPlayerProgressionData.LastPlayedLevelName = value;
            UpdateStoredData();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerProgressionService"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize/deserialize the stored payload.</param>
    /// <param name="storageHelper">The storage helper used to store and received the data.</param>
    public PlayerProgressionService(ISerializer serializer, IStorageHelper storageHelper)
    {
        m_serializer = serializer;
        m_storageHelper = storageHelper;

        m_latestPlayerProgressionData = GetCurrentlyStoredData();
    }

    /// <summary>
    /// Returns the currently stored data.
    /// </summary>
    private PlayerProgressionData GetCurrentlyStoredData()
    {
        string payloadFromStorage = m_storageHelper.GetData(StorageKey.PlayerProgression);

        bool playerDataExistent = !string.IsNullOrEmpty(payloadFromStorage);
        var playerProgressionData = playerDataExistent ?
           m_serializer.Deserialize<PlayerProgressionData>(payloadFromStorage) : new PlayerProgressionData();

        if (!playerDataExistent)
        {
            playerProgressionData.LastPlayedLevelName = "Level1";
        }

        return playerProgressionData;
    }

    /// <summary>
    /// Updates the currently stored data based on the last <see cref="PlayerProgressionData"/>.
    /// </summary>
    private void UpdateStoredData()
    {
        string serializedData = m_serializer.Serialize(m_latestPlayerProgressionData);

        m_storageHelper.StoreData(StorageKey.PlayerProgression, serializedData);
    }

#region completed levels

    /// <summary>
    /// Tracks a level, based on the given level name, as completed.
    /// </summary>
    /// <param name="nameOfCompletedLevel">The name of completed level.</param>
    public void TrackLevelAsCompleted(string nameOfCompletedLevel)
    {
        if (m_latestPlayerProgressionData.CompletedLevelNames == null)
        {
            m_latestPlayerProgressionData.CompletedLevelNames = new List<string>();
        }

        if (m_latestPlayerProgressionData.CompletedLevelNames.Contains(nameOfCompletedLevel))
        {
            // already stored
            return;
        }

        m_latestPlayerProgressionData.CompletedLevelNames.Add(nameOfCompletedLevel);
        UpdateStoredData();
    }

    /// <summary>
    /// Returns if a level with a given name was already completed.
    /// </summary>
    public bool IsLevelCompleted(string nameOfLevelToCheck)
    {
        return m_latestPlayerProgressionData.CompletedLevelNames != null &&
            m_latestPlayerProgressionData.CompletedLevelNames.Contains(nameOfLevelToCheck);
    }

#endregion
}