using Unity.Collections;
using Unity.Mathematics;

namespace Balma.WFC
{
    public struct Rules : IWFCRules
    {
        public TileKey airKey;
        public TileKey grassKey;

        public void ApplyInitialConditions<T>(ref WFCJob<T>.Data data, ref NativeList<PropagateStackHelper> propagateStack) where T : IWFCRules
        {
            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                for (int k = 0; k < data.staticDomain.size.z; k++)
                {
                    data.Hint(new int3(i,data.staticDomain.size.y - 1,k), airKey, ref propagateStack);
                }
            }
            
            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                for (int k = 1; k < data.staticDomain.size.z; k++)
                {
                    data.Hint(new int3(i,data.staticDomain.size.y - 1,k), airKey, ref propagateStack);
                }
            }

            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                for (int j = 1; j < data.staticDomain.size.y - 1; j++)
                {
                    data.Hint(new int3(i,j,0), airKey, ref propagateStack);
                    data.Hint(new int3(i,j,data.staticDomain.size.z - 1), airKey, ref propagateStack);
                }
            }
            
            for (int k = 1; k < data.staticDomain.size.z - 1; k++)
            {
                for (int j = 1; j < data.staticDomain.size.y - 1; j++)
                {
                    data.Hint(new int3(0,j,k), airKey, ref propagateStack);
                    data.Hint(new int3(data.staticDomain.size.x - 1,j,k), airKey, ref propagateStack);
                }
            }
            
            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                data.Hint(new int3(i,0,0), grassKey, ref propagateStack);
                data.Hint(new int3(i,0,data.staticDomain.size.z - 1), grassKey, ref propagateStack);
            }
            
            for (int k = 1; k < data.staticDomain.size.z - 1; k++)
            {
                data.Hint(new int3(0,0,k), grassKey, ref propagateStack);
                data.Hint(new int3(data.staticDomain.size.x - 1,0,k), grassKey, ref propagateStack);
            }
            
            // for (int i = 1; i < domain.size.x-1; i++)
            // {
            //     for (int k = 1; k < domain.size.z-1; k++)
            //     {
            //         WFCJob<Rules>.Hint(new int3(i,0,k), solidKey, ref domain);
            //     }
            // }
            
            //WFCJob<Rules>.Hint(new int3(0,0,0), grassKey, ref domain);
        }
    }
}