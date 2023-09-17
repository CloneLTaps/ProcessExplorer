namespace PluginInterface
{
    public class Settings
    {
        public bool RemoveZeros { get; set; }

        public bool TreatNullAsPeriod { get; set; }
        public bool OffsetsInHex { get; set; }

        public bool ReterunToTop { get; set; }

        public Settings(bool remozeZeros, bool treatNullAsPeriod, bool offsetsInHex, bool reterunToTop)
        {
            RemoveZeros = remozeZeros;
            TreatNullAsPeriod = treatNullAsPeriod;
            OffsetsInHex = offsetsInHex;
            ReterunToTop = reterunToTop;
        }
    }
}
