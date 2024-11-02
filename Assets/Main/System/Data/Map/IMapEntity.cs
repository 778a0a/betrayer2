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
        // 奇数行か偶数行かによって処理を分ける（Y座標が奇数か偶数かで判断）
        bool isOddRow = (y % 2) != 0;
        if (targetY < y)
        {
            if (isOddRow)
            {
                if (targetX <= x) return Direction.UpLeft;
                else return Direction.UpRight;
            }
            else
            {
                if (targetX < x) return Direction.UpLeft;
                else return Direction.UpRight;
            }
        }
        if (targetY > y)
        {
            if (isOddRow)
            {
                if (targetX <= x) return Direction.DownLeft;
                else return Direction.DownRight;
            }
            else
            {
                if (targetX < x) return Direction.DownLeft;
                else return Direction.DownRight;
            }
        }
        throw new InvalidOperationException();

        throw new InvalidOperationException();
    }

    public static int DistanceTo(this IMapEntity self, IMapEntity target)
    {
        var a = OffsetToCube(self.Position.x, self.Position.y);
        var b = OffsetToCube(target.Position.x, target.Position.y);
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));

        static Vector3Int OffsetToCube(int col, int row)
        {
            int x = col - (row - (row & 1)) / 2;
            int z = row;
            int y = -x - z;
            return new(x, y, z);
        }
    }
}