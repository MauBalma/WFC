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
        public int3 size;//TODO move to static domain
        public int tileCount;//TODO move to static domain

        public NativeList<WFCTileData2> tileDatas;//TODO move to static domain
        public NativeList<float> tileWeight;//TODO move to static domain

        public Random rng;
        public NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
        public DecreseableMinHeap<int3> open;
        public NativeReference<int3> contradiction;
    }
    
    
}