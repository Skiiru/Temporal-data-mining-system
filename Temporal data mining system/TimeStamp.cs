namespace Temporal_data_mining_system
{
    [System.Serializable()]
    public class TimeStamp
    {
        public string Type { get; set; }
        public string OriginalText { get; set; }
        public int Index { get; set; }

        public TimeStamp(string type, string text, int index)
        {
            Type = type;
            OriginalText = text;
            Index = index;
        }
    }
}
