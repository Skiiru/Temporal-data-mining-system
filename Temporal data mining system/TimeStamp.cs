namespace Temporal_data_mining_system
{
    class TimeStamp
    {
        public string Type { get; set; }
        public string OriginalText { get; set; }
        public int Index { get; set; }

        public TimeStamp(string type, string text, int index)
        {
            this.Type = type;
            this.OriginalText = text;
            this.Index = index;
        }
    }
}
