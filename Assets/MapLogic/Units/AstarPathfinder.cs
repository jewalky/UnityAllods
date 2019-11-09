using System;
using System.Collections.Generic;
using SearchPathApi;
using UnityEngine;

class AstarPathfinder : ISearchAlgorithm<Vector2i>
{
    private readonly MapUnit mapUnit;
    private bool found = false;
    private int limit;
    private SearchContext<Vector2i> ctx;

    public AstarPathfinder(MapUnit mapUnit, int limit = -1)
    {
        this.mapUnit = mapUnit;
        this.limit = limit;
    }

    private class AstarNode
    {
        public AstarNode Parent;
        public int X;
        public int Y;
        public int GScore;
        public int FScore;

        public AstarNode()
        {
            /* ... */
        }

        public AstarNode(AstarNode parent, int x, int y, int gscore, int fscore)
        {
            Parent = parent;
            X = x;
            Y = y;
            GScore = gscore;
            FScore = fscore;
        }

        public static uint XYToUint(int x, int y)
        {
            return ((uint)(x & 0xFFFF) | ((uint)(y & 0xFFFF) << 16));
        }
    }

    private int Heuristic(int x, int y, int targetX, int targetY)
    {
        return (int)(new Vector2i(x - targetX, y - targetY).magnitude*10);
    }

    private List<Vector2i> GetPath(AstarNode node)
    {
        List<Vector2i> path = new List<Vector2i>();
        AstarNode parent = node;
        while (true)
        {
            if (parent.Parent == null)
                break; // don't add first node
            path.Insert(0, new Vector2i(parent.X, parent.Y));
            parent = parent.Parent;
        }
        return path;
    }

    private int GetCost(int cellX, int cellY, int nx, int ny)
    {
        if ((nx != 0) == (ny != 0)) // diagonal
            return 15;
        return 10;
    }

    private bool CheckCell(MapUnit unit, int x, int y, bool staticOnly)
    {
        return unit.Interaction.CheckWalkableForUnit(x, y, staticOnly);
    }
    
    // Note: should be the same as the expression above Getpath() call
    // it's duplicated here so that we don't calculate toCenterX, toCenterY every iteration
    public static bool IsFinalNode(int x, int y, int toStartX, int toStartY, int toEndX, int toEndY, float distance)
    {
        int toCenterX = (toEndX + toStartX) / 2;
        int toCenterY = (toEndY + toStartY) / 2;
        return ((x >= toStartX && x <= toEndX && y >= toStartY && y <= toEndY) || // if path is directly contained in the goal list
                (new Vector2i(x - toCenterX, y - toCenterY).magnitude <= distance)); // or distance is enough
    }

    public List<Vector2i> FindPath(MapUnit unit, int x, int y, int limit = -1)
    {
        LinkedList<AstarNode> openNodes = new LinkedList<AstarNode>();
        HashSet<uint> closedNodes = new HashSet<uint>();
        int numOpenNodes = 1;

        openNodes.AddFirst(new AstarNode(null, x, y, 0, ctx.estimateCost(new Vector2i(x,y))));

        int searchFromX = 0;
        int searchFromY = 0;
        int searchToX = 256;
        int searchToY = 256;

        if (limit >= 0)
        {
            searchFromX = x - limit;
            searchFromY = y - limit;
            searchToX = x + limit;
            searchToY = y + limit;
        }

        // deviation from origin allowed: 128000 / size of traversed nodes

        // add start node
        int checkedNodes = 0;
        while (openNodes.First != null)
        {
            checkedNodes++;
            LinkedListNode<AstarNode> node = openNodes.First;
            // first node is guaranteed to have lowest heuristic
            // check if this node is Goal
            AstarNode val = node.Value;
            if (ctx.isFinalState(new Vector2i(val.X, val.Y))) // or distance is enough
            {
                found = true;
                return GetPath(val);
            }

            // not goal, advance the list
            openNodes.RemoveFirst();
            closedNodes.Add(AstarNode.XYToUint(val.X, val.Y));

            // check deviation
            int dev = Math.Max(Math.Abs(val.X-x), Math.Abs(val.Y-y));
            if (dev > 256000 / numOpenNodes)
                continue; // do not traverse this node at all

            // find neighbor nodes
            for (int nx = -1; nx <= 1; nx++)
            {
                for (int ny = -1; ny <= 1; ny++)
                {
                    if (nx == 0 && ny == 0)
                        continue;

                    // get coords
                    int offsX = val.X + nx;
                    int offsY = val.Y + ny;

                    // check limit
                    if (offsX < searchFromX || offsX > searchToX ||
                        offsY < searchFromY || offsY > searchToY) continue;

                    // check closed list
                    if (closedNodes.Contains(AstarNode.XYToUint(offsX, offsY)))
                        continue;

                    // check walkability
                    if (!ctx.isWalkable(new Vector2i(val.X, val.Y), new Vector2i(offsX, offsY)))
                    {
                        closedNodes.Add(AstarNode.XYToUint(offsX, offsY));
                        continue; // do not traverse this node
                    }

                    // check open list
                    int gscore = val.GScore + GetCost(val.X, val.Y, nx, ny);
                    LinkedListNode<AstarNode> nnode = openNodes.First;
                    AstarNode existing = null;
                    while (nnode != null)
                    {
                        AstarNode c = nnode.Value;
                        if (c.X == offsX && c.Y == offsY)
                        {
                            existing = c;
                            break;
                        }
                        nnode = nnode.Next;
                    }

                    // replace node if GScore is lower
                    if (nnode == null || gscore < existing.GScore)
                    {
                        if (existing == null)
                            existing = new AstarNode();
                        existing.Parent = val;
                        existing.X = offsX;
                        existing.Y = offsY;
                        existing.GScore = gscore;
                        existing.FScore = existing.GScore + ctx.estimateCost(new Vector2i(offsX, offsY));

                        if (nnode != null)
                            openNodes.Remove(nnode);

                        // for now re-add again with new cost
                        // in theory, we can just move it up. for lower cost
                        LinkedListNode<AstarNode> mnode = openNodes.First;
                        while (mnode != null)
                        {
                            AstarNode c = mnode.Value;
                            if (c.FScore >= existing.FScore)
                            {
                                openNodes.AddBefore(mnode, existing);
                                break;
                            }

                            mnode = mnode.Next;
                        }

                        // highest FScore of whole list
                        if (mnode == null)
                            openNodes.AddLast(existing);
                        numOpenNodes++;
                    }

                }
            }
        }

        return null;

    }

    public ISearchResult<Vector2i> search(SearchContext<Vector2i> ctx)
    {
        var start = ctx.start;
        limit = 16;
        this.ctx = ctx; 
        var path = FindPath(mapUnit, start.x, start.y, limit);
        SearchState searchState;
        if (path == null)
            searchState = SearchState.NONE;
        else
            searchState = SearchState.FINISHED;
        if (found)
        {
            searchState |= SearchState.FOUND;
        }

        return new StandardSearchResult<Vector2i>(searchState, path);
    }
}

