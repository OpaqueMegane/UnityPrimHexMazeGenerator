using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMazeGenerator : MonoBehaviour
{
    public Vector2Int mazeDimensions = new Vector2Int(16, 16);
    public int cellDistance = 3;
    public bool markNodeTiles = false;

    Tilemap _tilemap;

    public TileBase _pathTile;
    public TileBase _wallTile;
    public TileBase _nodeTile;

    class MazeNode
    {
        public Vector2Int coord;
        public List<MazeNode> connections = new List<MazeNode>();
        public MazeNode(Vector2Int loc)
        {
            coord = loc;
        }
    }


    void Start()
    {
        _tilemap = this.GetComponentInChildren<Tilemap>();

        for (int xi = -1; xi <= (mazeDimensions.x - 1) * cellDistance + 1; xi++)
        {
            for (int yi = -1; yi <= (mazeDimensions.y - 1) * cellDistance + 1; yi++)
            {
                _tilemap.SetTile(new Vector3Int(xi, yi, 0), _wallTile);
            }
        }

        //All the nodes, and their connection that made up the maze, 
        //Is dictionary so can look up by grid coordinate.
        Dictionary<Vector2Int, MazeNode> mazeNodes = new Dictionary<Vector2Int, MazeNode>();

        
        
        ////--- Center the camera on the maze
        //Vector2 mazeCenter = mazeDimensions * cellDistance / 2;
        //Camera.main.transform.position += new Vector3(mazeCenter.x, mazeCenter.y, 0);


        //--- Pick a random starting cell on the maze, and add it to the maze
        Vector2Int firstMazeCoord = new Vector2Int(Random.Range(0, mazeDimensions.x), Random.Range(0, mazeDimensions.y));
        var startingMazeNode = new MazeNode(firstMazeCoord);
        mazeNodes.Add(firstMazeCoord, startingMazeNode);

        var mm = GameObject.FindObjectOfType<SimpleMazeMover>();
        if (mm != null)
        {
            mm.transform.position = new Vector3(firstMazeCoord.x, firstMazeCoord.y, 0);
        }

        //-- Get the frontier (cells not yet in the maze, but adjcent to existing maze cells)
        List<Vector2Int> frontier = new List<Vector2Int>();
        frontier.AddRange(getNeighbors(firstMazeCoord));

        //-- Keep track of every cell we've considered in the process
        List<Vector2Int> visited = new List<Vector2Int>();
        visited.Add(firstMazeCoord);

        int sanity = 9999; //prevent an infinite loop, probably not necessary anymore

        while (frontier.Count > 0 && sanity > 0)
        {
            sanity--;
            
            //Pick a random frontier cell, and mark visitied
            Vector2Int randomFrontierCoord = frontier[Random.Range(0, frontier.Count)];
            visited.Add(randomFrontierCoord);

            //Find existing maze node that leads to this frontier cell...
            List<Vector2Int> neighbors = getNeighbors(randomFrontierCoord);
            int randOffset = Random.Range(0, neighbors.Count);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int neighb = neighbors[(i + randOffset) % neighbors.Count];
                if (mazeNodes.ContainsKey(neighb)) //Found it!
                {
                    //Remove the cell from frontier...
                    frontier.Remove(randomFrontierCoord); 
      
                    //Add it to the maze, connected the previous maze node
                    MazeNode newNode = new MazeNode(randomFrontierCoord);
                    mazeNodes[randomFrontierCoord] = newNode;
                    mazeNodes[neighb].connections.Add(newNode);

                    //Add the new maze node's adjacents cells to the frontier
                    foreach (var neighborCoord in neighbors)
                    {
                        if (!mazeNodes.ContainsKey(neighborCoord) && !frontier.Contains(neighborCoord) && !visited.Contains(neighborCoord))
                        {
                            frontier.Add(neighborCoord);
                        }
                    }

                    //only connect in 1 place
                    break;
                }
            }
        }

       //starting from the first mode, recursively fill in maze paths
        fillInTraversableTilesRecursive(startingMazeNode);
    }

    public float noiseOverdrive = 1;
    Vector3Int getNoisyLoc(MazeNode node, Vector3Int cleanLoc)
    {
        float noiseFreq = 100;
        float noiseXf =  Mathf.PerlinNoise(
            noiseFreq * node.coord.x / mazeDimensions.x,
            noiseFreq * node.coord.y / mazeDimensions.y);

        float noiseYf =  Mathf.PerlinNoise(
         1 - (.99f * noiseFreq * node.coord.x / mazeDimensions.x),
         1.01f * noiseFreq * node.coord.y / mazeDimensions.y);

        Vector3Int nodeCoordV3 = new Vector3Int(node.coord.x * cellDistance, node.coord.y * cellDistance, 0);
        int noiseX = (int) (this.cellDistance * noiseOverdrive * Mathf.Lerp(-1, 1, noiseXf));
        int noiseY = (int) (this.cellDistance * noiseOverdrive * Mathf.Lerp(-1, 1, noiseYf));
        nodeCoordV3.x += noiseX;
        nodeCoordV3.y += noiseY;
        return nodeCoordV3;

    }

    void fillInTraversableTilesRecursive(MazeNode node)
    {


        Debug.LogError(node.coord);

        Vector3Int nodeCoordV3 = new Vector3Int(node.coord.x * cellDistance, node.coord.y * cellDistance, 0);
        nodeCoordV3 = getNoisyLoc(node, nodeCoordV3);
        //Draw the tile at the node...
        _tilemap.SetTile(nodeCoordV3, markNodeTiles ? _nodeTile : _pathTile);

        foreach (var connectedNode in node.connections)
        {
            Vector3Int connectedNodeCoordV3 = new Vector3Int(connectedNode.coord.x * cellDistance, connectedNode.coord.y * cellDistance, 0);
            connectedNodeCoordV3 = getNoisyLoc(connectedNode, connectedNodeCoordV3);

            bool incX = Random.Range(0, 3) == 1;//connectedNode.coord.x % 2 == 0;
            
            //Draw the tiles from this node, up to each connected node, alternating x/y
            Vector3Int intermediate = nodeCoordV3;
            int nDeviations = cellDistance ;// 3 * Random.Range(0, cellDistance);
            Vector3Int lastMove = Vector3Int.zero;
            bool prevDev = false;
            while (intermediate != connectedNodeCoordV3)
            {
                incX = Random.Range(0, 3) == 1;
                incX |= intermediate.y == connectedNodeCoordV3.y;

                
                bool deviate = nDeviations > 0 && Random.Range(0f, 1f) < 5f / cellDistance;
                var prev = intermediate;
                prevDev = false;
                /*if (deviate)
                {
                    prevDev = true;
                    nDeviations--;



                    if (Mathf.Abs(intermediate.x - connectedNodeCoordV3.x) < Mathf.Abs(intermediate.y - connectedNodeCoordV3.y))
                    {
                        incX = false;

                        int diff = (int)-Mathf.Sign(connectedNode.coord.x - intermediate.x);
                        intermediate.x += diff != 0 ? diff : (Random.Range(0, 2) * 2) - 1;
                    }
                    else
                    {
                        incX = true;
                        int diff = (int)-Mathf.Sign(connectedNode.coord.y - intermediate.y);
                        intermediate.y += diff != 0 ? diff : (Random.Range(0, 2) * 2) - 1;
                    }
                } 
                else */
                
                if (incX && intermediate.x != connectedNodeCoordV3.x)
                {
                    intermediate.x = (int) Mathf.MoveTowards(intermediate.x, connectedNodeCoordV3.x, 1);
                    incX = false;
                }
                else //if (intermediate.y != connectV3.x)
                {
                    intermediate.y = (int)Mathf.MoveTowards(intermediate.y, connectedNodeCoordV3.y, 1);
                    incX = true;
                }
                lastMove = intermediate - prev;



                if (intermediate != connectedNodeCoordV3)
                {
                    _tilemap.SetTile(intermediate, _pathTile);
                }
            }

            //repeat again for the connected node
            fillInTraversableTilesRecursive(connectedNode);

        }

    }


    List<Vector2Int> getNeighbors(Vector2Int v)
    {
        int x = v.x;
        int y = v.y;
        List<Vector2Int> neighbors = new List<Vector2Int>();

        bool atXmin = x == 0;
        bool atXmax = x == mazeDimensions.x - 1;

        bool atYmin = y == 0;
        bool atYmax = y == mazeDimensions.y - 1;

        if (!atYmin)
        {
            neighbors.Add(new Vector2Int(x, y - 1));
        }

        if (!atYmax)
        {
            neighbors.Add(new Vector2Int(x, y + 1));
        }

        if (!atXmin)
        {
            neighbors.Add(new Vector2Int(x - 1, y));

            if (y % 2 == 0)
            {
                if (!atYmin)
                {
                    neighbors.Add(new Vector2Int(x - 1, y - 1));
                }

                if (!atYmax)
                {
                    neighbors.Add(new Vector2Int(x - 1, y + 1));
                }
            }
        }

        if (!atXmax)
        {
            neighbors.Add(new Vector2Int(x + 1, y));


            if (y % 2 == 1)
            {
                if (!atYmin)
                {
                    neighbors.Add(new Vector2Int(x + 1, y - 1));
                }

                if (!atYmax)
                {
                    neighbors.Add(new Vector2Int(x + 1, y + 1));
                }
            }
        }

        return neighbors;
    }

    //List<Vector2Int> getOppositeNeighborPairs(Vector2Int v)
    //{
    //    int x = v.x;
    //    int y = v.y;
    //    List<Vector2Int> neighbors = new List<Vector2Int>();

    //    bool atXmin = x == 0;
    //    bool atXmax = x == _gridDim.x - 1;

    //    bool atYmin = y == 0;
    //    bool atYmax = y == _gridDim.y - 1;

    //    if (!atXmin && !atXmax) // E - W
    //    {
    //        neighbors.Add(new Vector2Int(x - 1, y));
    //        neighbors.Add(new Vector2Int(x + 1, y));
    //    }


    //    if (!atYmin && !atYmax)
    //    {
    //        if (y % 2 == 0 && !atXmin)
    //        {
    //            neighbors.Add(new Vector2Int(x, y - 1));
    //            neighbors.Add(new Vector2Int(x - 1, y + 1));

    //            neighbors.Add(new Vector2Int(x - 1, y - 1));
    //            neighbors.Add(new Vector2Int(x, y + 1));
    //        }

    //        if (y % 2 == 1 && !atXmax)
    //        {
    //            neighbors.Add(new Vector2Int(x, y - 1));
    //            neighbors.Add(new Vector2Int(x + 1, y + 1));

    //            neighbors.Add(new Vector2Int(x + 1, y - 1));
    //            neighbors.Add(new Vector2Int(x, y + 1));
    //        }
    //    }






    //    return neighbors;
    //}


}
