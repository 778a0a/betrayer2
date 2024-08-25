public class GameMapTile
{
    public MapPosition Position { get; }
    public HexTile UI { get; }
    public Terrain Terrain { get; }
    
    public Castle Castle { get; }
    public Town Town { get; }
    public Country Country => (Town.Castle ?? Castle).Country;

    public GameMapTile(MapPosition pos, HexTile ui, Terrain terrain)
    {
        Position = pos;
        UI = ui;
        Terrain = terrain;

        Castle = new Castle() { Position = pos };
        Town = new Town() { Position = pos };
    }
}
