using Unity.Burst;
using Unity.Collections;
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
                data.Observe(data.domain.open.Pop(out var entropy));
                if(entropy < 0) continue;
                if(data.domain.contradiction.Value) return;//Abort
            }
        }
    }
    
    [BurstCompile]
    public struct WFCInitializeJob<TWFCRules> : IJob where TWFCRules : IWFCRules
    {
        private TWFCRules rules;
        private WFCData data;

        public WFCInitializeJob(TWFCRules rules, ref WFCStaticDomain staticDomain, ref WFCDomain domain)
        {
            this.rules = rules;
            data.domain = domain;
            data.staticDomain = staticDomain;
        }

        public void Execute()
        {
            data.domain.InitializeClean(data.staticDomain);
            rules.ApplyInitialConditions(ref data);
        }
    }
    
    [BurstCompile]
    public struct WFCResolveStepJob : IJob
    {
        public NativeReference<int3> observedCoordinates;
        private WFCData data;

        public WFCResolveStepJob(ref WFCStaticDomain staticDomain, ref WFCDomain domain, ref NativeReference<int3> observedCoordinates)
        {
            data.domain = domain;
            data.staticDomain = staticDomain;
            this.observedCoordinates = observedCoordinates;
        }

        public void Execute()
        {
            observedCoordinates.Value = data.domain.open.Pop(out var entropy);
            if(entropy < 0) return;
            data.Observe(observedCoordinates.Value);
        }
    }
}