namespace MidiDriverOrderForKorg
{
    public class RegistryEntry
    {
        public string FullKey { get; set; }
        public string Alias { get; set; }
        public string DeviceName { get; set; }
        public bool IsKorg { get; set; }
        public string Driver { get; set; }
        public bool IsLocked { get; set; }

        // create constructor 
        public RegistryEntry(string alias, string deviceName, string driver, string fullKey, bool isKorg, bool isLocked)
        {
            Alias = alias;
            FullKey = fullKey;
            DeviceName = deviceName;
            IsKorg = isKorg;
            Driver = driver;
            IsLocked = isLocked;
        }

    }
}