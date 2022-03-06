using System;
using Balma.ADT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Balma.WFC
{
    public struct WFCStaticDomain : IDisposable
    {
        public int3 size;
        public int tileCount;

        public NativeList<WFCTileData> tileDatas;
        public NativeList<float> tileWeight;
        
        public void Dispose()
        {
            tileDatas.Dispose();
            tileWeight.Dispose();
        }
    }

    public struct WFCDomain : IDisposable
    {
        public Random rng;
        public NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
        public MinHeap<int3> open;
        public NativeReference<bool> contradiction;
        public NativeList<WFCPropagateStackHelper> propagateStack;
        
        public void InitializeClean(WFCStaticDomain staticDomain)
        {
            contradiction.Value = false;
            open.Clear();
            propagateStack.Clear();
                    
            //TODO IJobFor to clean possible
            for (var i = 0; i < staticDomain.size.x; i++)
            for (var j = 0; j < staticDomain.size.y; j++)
            for (var k = 0; k < staticDomain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);
        
                var possible = possibleTiles[coordinate];
                possible.Clear();
        
                for (var tileIndex = 0; tileIndex < staticDomain.tileCount; tileIndex++)
                {
                    possible.Add(new TileKey(){index = tileIndex});
                }
        
                possibleTiles[coordinate] = possible;//Reassign, is a struct not a reference
                open.Push(coordinate, 1);
            }
        }
        
        public void Dispose()
        {
            using var possiblesList = possibleTiles.GetEnumerator();
            while (possiblesList.MoveNext()) possiblesList.Current.Value.Dispose();
            possibleTiles.Dispose();
            open.Dispose();
            contradiction.Dispose();
            propagateStack.Dispose();
        }
    }

}