using Balma.ADT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Balma.WFC
{
    public struct WFCDomain
    {
        public int3 size;
        public Random rng;
            
        public int tileCount;
        public NativeList<int> prefabIndex;
        public NativeList<Quaternion> prefabRotation;
        public NativeList<WFCTileData2> tileDatas;
        public NativeList<float> tileWeight;
            
        public DecreseableMinHeap<int3> open;
        public NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
    }
}