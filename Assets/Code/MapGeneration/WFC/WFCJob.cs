using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace Balma.WFC
{
    [BurstCompile]
    public struct WFCResolveAllJob<TWFCRules> : IJob where TWFCRules : IWFCRules
    {
        private TWFCRules rules;
        private WFCData data;

        public WFCResolveAllJob(TWFCRules rules, ref WFCStaticDomain staticDomain, ref WFCDomain domain)
        {
            this.rules = rules;
            data.domain = domain;
            data.staticDomain = staticDomain;
        }

        public void Execute()
        {
            data.domain.InitializeClean(data.staticDomain);
            rules.ApplyInitialConditions(ref data);

            while (data.domain.open.Count > 0)
            {
                data.Observe(data.domain.open.Pop());
                if(data.domain.contradiction.Value) return;//Abort
            }
        }
    }
}