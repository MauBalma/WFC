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
        public int tileCount;//TODO calculate form size

        public NativeList<int> prefabIndex;//TODO move outside of domain, not used in the job
        public NativeList<Quaternion> prefabRotation;//TODO move outside of domain, not used in the job

        public NativeList<WFCTileData2> tileDatas;//TODO move to static domain
        public NativeList<float> tileWeight;//TODO move to static domain

        public NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
        public DecreseableMinHeap<int3> open;
        public Random rng;
        public NativeReference<int3> contradiction;

        public NativeList<PropagateStackHelper> propagateStack;//TODO move outside of domain  to job field

        public WFCDomain Copy(Allocator allocator)
        {
            var copy = new WFCDomain();

            copy.size = this.size;
            copy.rng = this.rng;
            copy.tileCount = this.tileCount;

            return copy;
        }
    }
    
    
}