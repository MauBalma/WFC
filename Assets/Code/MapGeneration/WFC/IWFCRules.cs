namespace Balma.WFC
{
    public interface IWFCRules
    {
        void ApplyInitialConditions(ref WFCData data);
    }
}