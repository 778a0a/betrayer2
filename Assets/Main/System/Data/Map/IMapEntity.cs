using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface IMapEntity
{
    MapPosition Position { get; }
}

public static class MapEntityExtensions
{
    public static Direction DirectionTo(this IMapEntity self, IMapEntity target)
    {
        var x = self.Position.x;
        var y = self.Position.y;
        var targetX = target.Position.x;
        var targetY = target.Position.y;

        if (targetY == y)
        {
            if (targetX < x) return Direction.Left;
            else return Direction.Right;
        }
        if (targetY < y)
        {
            if (targetX < x) return Direction.UpLeft;
            else return Direction.UpRight;
        }
        if (targetY > y)
        {
            if (targetX < x) return Direction.DownLeft;
            else return Direction.DownRight;
        }
        throw new InvalidOperationException();
    }

    public static float DistanceTo(this IMapEntity self, IMapEntity target)
    {
        var dx = target.Position.x - self.Position.x;
        var dy = target.Position.y - self.Position.y;
        if (dy % 2 == 0)
        {
            return Mathf.Abs(dx) + Mathf.Abs(dy) / 2;
        }
        else
        {
            return Mathf.Abs(dx) + (Mathf.Abs(dy) - 1) / 2;
        }
    }
}