using Unity.Mathematics;

namespace Balma.WFC
{
    public struct Rules : IWFCRules
    {
        public TileKey airKey;
        public TileKey grassKey;
        
        public void ApplyInitialConditions(ref WFCDomain domain)
        {
            for (int i = 0; i < domain.size.x; i++)
            {
                for (int k = 0; k < domain.size.z; k++)
                {
                    WFCJob<Rules>.Hint(new int3(i,domain.size.y - 1,k), airKey, ref domain);
                }
            }
            
            for (int i = 0; i < domain.size.x; i++)
            {
                for (int k = 1; k < domain.size.z; k++)
                {
                    WFCJob<Rules>.Hint(new int3(i,domain.size.y - 1,k), airKey, ref domain);
                }
            }

            for (int i = 0; i < domain.size.x; i++)
            {
                for (int j = 1; j < domain.size.y - 1; j++)
                {
                    WFCJob<Rules>.Hint(new int3(i,j,0), airKey, ref domain);
                    WFCJob<Rules>.Hint(new int3(i,j,domain.size.z - 1), airKey, ref domain);
                }
            }
            
            for (int k = 1; k < domain.size.z - 1; k++)
            {
                for (int j = 1; j < domain.size.y - 1; j++)
                {
                    WFCJob<Rules>.Hint(new int3(0,j,k), airKey, ref domain);
                    WFCJob<Rules>.Hint(new int3(domain.size.x - 1,j,k), airKey, ref domain);
                }
            }
            
            for (int i = 0; i < domain.size.x; i++)
            {
                WFCJob<Rules>.Hint(new int3(i,0,0), grassKey, ref domain);
                WFCJob<Rules>.Hint(new int3(i,0,domain.size.z - 1), grassKey, ref domain);
            }
            
            for (int k = 1; k < domain.size.z - 1; k++)
            {
                WFCJob<Rules>.Hint(new int3(0,0,k), grassKey, ref domain);
                WFCJob<Rules>.Hint(new int3(domain.size.x - 1,0,k), grassKey, ref domain);
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