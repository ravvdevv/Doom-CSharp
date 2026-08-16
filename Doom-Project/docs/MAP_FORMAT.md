# Map Format

## File Location

```
resources/maps/map.txt
```

## Format

The map is a plain text file. Each character represents one tile on the grid.

- **Grid size**: 42 columns × 32 rows
- **Each character**: one tile
- **No separators** between characters

## Tile Types

| Character | Meaning | Rendered As |
|-----------|---------|-------------|
| `0` | Empty space (floor) | Dark floor gradient |
| `1` | Wall | Textured wall (uses `resources/textures/1.png`) |

## Example

```
111111111111111111111111111111111111111111
100000000000000000000000000000000000000001
100000000000000000000000000000000000000001
100000000000000000000000000000000000000001
100000111000000000000000000000000000000001
100000111000000000000000000000000000000001
100000111000000000000000000000000000000001
100000000000000000000000000000000000000001
...
111111111111111111111111111111111111111111
```

- The border of `1`s forms the outer walls
- `0`s are walkable floor space
- Inner `1` blocks form rooms and corridors

## How the Map Is Loaded

In `Loader.cs`, the `LoadMap()` function reads the text file and converts it to a 2D character array:

```csharp
char[,] map = new char[rows, cols];
map[r, c] = lines[r][c];  // Each character becomes one cell
```

## How Walls Are Checked

When the player or an enemy tries to move, the code checks:

```csharp
if (_map[row, col] == '1')
{
    // It's a wall — can't move here
}
```

When the raycaster draws walls, it steps through the map grid until it hits a `1`:

```csharp
hit = _map[mapY, mapX];  // Keep stepping until we hit '1'
```

## Coordinate System

- **Row (r)**: vertical position (top = 0, bottom = 31)
- **Column (c)**: horizontal position (left = 0, right = 41)
- **Player position** uses decimal values: `1.5, 1.5` means center of tile at row 1, column 1

## Editing the Map

1. Open `resources/maps/map.txt` in any text editor
2. Change `0` to `1` to add walls, or `1` to `0` to remove walls
3. Keep the grid the same size (42 columns × 32 rows)
4. Save the file and restart the game

**Important**: Every row must be exactly 42 characters. Every row must be the same length.
