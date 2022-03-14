namespace OpenRiaServices.Hosting.Wcf
{
    internal class FaultException<T>
    {
        public FaultException(DomainServiceFault fault, FaultReason faultReason)
        {
            Fault = fault;
            FaultReason = faultReason;
        }

        public DomainServiceFault Fault { get; }
        public FaultReason FaultReason { get; }
    }
}