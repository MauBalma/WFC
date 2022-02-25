using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace Balma.WFC
{
    [BurstCompile]
    public struct WFCJob<TWFCRules> : IJob where TWFCRules : IWFCRules
    {
        private TWFCRules rules;
        private WFCData data;

        public WFCJob(TWFCRules rules, ref WFCStaticDomain staticDomain, ref WFCDomain domain)
        {
            this.rules = rules;
            data.domain = domain;
            data.staticDomain = staticDomain;
        }

        public void Execute()
        {
            InitializeClean();
            rules.ApplyInitialConditions(ref data);

            while (data.domain.open.Count > 0)
            {
                data.Observe(data.domain.open.Pop());
                if(data.domain.contradiction.Value) return;//Abort
            }
        }

        private void InitializeClean()
        {
            data.domain.contradiction.Value = false;
            data.domain.open.Clear();
            data.domain.propagateStack.Clear();
            
            for (var i = 0; i < data.staticDomain.size.x; i++)
            for (var j = 0; j < data.staticDomain.size.y; j++)
            for (var k = 0; k < data.staticDomain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);

                var possible = data.domain.possibleTiles[coordinate];
                possible.Clear();

                for (var tileIndex = 0; tileIndex < data.staticDomain.tileCount; tileIndex++)
                {
                    possible.Add(new TileKey(){index = tileIndex});
                }

                data.domain.possibleTiles[coordinate] = possible;//Reassign, is a struct not a reference
                data.domain.open.Push(coordinate, 1);
            }
        }
    }
}