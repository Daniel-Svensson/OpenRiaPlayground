using System.Globalization;

namespace OpenRiaServices.Hosting.Wcf
{
    internal class FaultReasonText
    {
        private string v;
        private CultureInfo currentCulture;

        public FaultReasonText(string v, CultureInfo currentCulture)
        {
            this.v = v;
            this.currentCulture = currentCulture;
        }
    }
}