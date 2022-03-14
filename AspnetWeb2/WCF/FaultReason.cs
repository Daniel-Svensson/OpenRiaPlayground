namespace OpenRiaServices.Hosting.Wcf
{
    internal class FaultReason
    {
        private FaultReasonText faultReasonText;

        public FaultReason(FaultReasonText faultReasonText)
        {
            this.faultReasonText = faultReasonText;
        }
    }
}