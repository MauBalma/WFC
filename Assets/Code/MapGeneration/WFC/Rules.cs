﻿using Unity.Collections;
using Unity.Mathematics;

namespace Balma.WFC
{
    public struct Rules : IWFCRules
    {
        public TileKey airKey;
        public TileKey grassKey;

        public void ApplyInitialConditions<T>(ref WFCJob<T>.Data data) where T : IWFCRules
        {
            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                for (int k = 0; k < data.staticDomain.size.z; k++)
                {
                    data.Hint(new int3(i,data.staticDomain.size.y - 1,k), airKey, ref data);
                }
            }
            
            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                for (int k = 1; k < data.staticDomain.size.z; k++)
                {
                    data.Hint(new int3(i,data.staticDomain.size.y - 1,k), airKey, ref data);
                }
            }

            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                for (int j = 1; j < data.staticDomain.size.y - 1; j++)
                {
                    data.Hint(new int3(i,j,0), airKey, ref data);
                    data.Hint(new int3(i,j,data.staticDomain.size.z - 1), airKey, ref data);
                }
            }
            
            for (int k = 1; k < data.staticDomain.size.z - 1; k++)
            {
                for (int j = 1; j < data.staticDomain.size.y - 1; j++)
                {
                    data.Hint(new int3(0,j,k), airKey, ref data);
                    data.Hint(new int3(data.staticDomain.size.x - 1,j,k), airKey, ref data);
                }
            }
            
            for (int i = 0; i < data.staticDomain.size.x; i++)
            {
                data.Hint(new int3(i,0,0), grassKey, ref data);
                data.Hint(new int3(i,0,data.staticDomain.size.z - 1), grassKey, ref data);
            }
            
            for (int k = 1; k < data.staticDomain.size.z - 1; k++)
            {
                data.Hint(new int3(0,0,k), grassKey, ref data);
                data.Hint(new int3(data.staticDomain.size.x - 1,0,k), grassKey, ref data);
            }
        }
    }
}