using Balma.ADT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Balma.WFC
{
    public struct WFCStaticDomain
    {
        public int3 size;
        public int tileCount;

        public NativeList<WFCTileData> tileDatas;
        public NativeList<float> tileWeight;
    }

    public struct WFCDomain
    {
        public Random rng;
        public NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
        public DecreseableMinHeap<int3> open;
        public NativeReference<bool> contradiction;
        public NativeList<PropagateStackHelper> propagateStack;
    }

}