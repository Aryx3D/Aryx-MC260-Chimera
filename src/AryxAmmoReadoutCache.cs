using System.Globalization;

namespace Aryx_MC260_Chimera
{
    internal struct AryxAmmoReadoutCache
    {
        private int amount;
        private int capacity;
        private bool magazine;
        private CultureInfo culture;
        private string text;

        public string Get(int amount, int capacity, bool magazine)
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            if (text == null || this.amount != amount || this.magazine != magazine || (magazine && this.capacity != capacity) || !ReferenceEquals(culture, currentCulture) || !currentCulture.IsReadOnly)
            {
                this.amount = amount;
                this.capacity = capacity;
                this.magazine = magazine;
                culture = currentCulture;
                text = magazine ? amount.ToString(currentCulture) + "/" + capacity.ToString(currentCulture) : amount.ToString(currentCulture);
            }
            return text;
        }
    }

}
