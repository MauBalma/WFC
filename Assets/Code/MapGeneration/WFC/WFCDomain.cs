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
        public DecreseableMinHeap<int3> open;
        public NativeReference<bool> contradiction;
        public NativeList<WFCPropagateStackHelper> propagateStack;
        
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