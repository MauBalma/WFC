namespace Balma.WFC
{
    public interface IWFCRules
    {
        void ApplyInitialConditions<T>(ref WFCJob<T>.Data data) where T : IWFCRules;
    }
}