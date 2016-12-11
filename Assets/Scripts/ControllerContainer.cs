#pragma warning disable 0649
/// <summary>
/// Container class that holds static instances of all controller and provides easy to them.
/// </summary>
public static class ControllerContainer
{
    private static MapTileGenerator m_mapTileGenerator;
    public static MapTileGenerator MapTileGenerator { get { return m_mapTileGenerator ?? new MapTileGenerator(); } }
}
