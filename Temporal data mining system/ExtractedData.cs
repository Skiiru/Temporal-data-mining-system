using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Temporal_data_mining_system
{
    class ExtractedData
    {
        [System.ComponentModel.DisplayName("Дата")]
        public string Date { get; set; }

        [System.ComponentModel.DisplayName("Объект")]
        public string Object { get; set; }

        public List<Word> objects = new List<Word>();

        [System.ComponentModel.DisplayName("Тенденция")]
        public string Trend { get; set; }

        public List<Word> trends = new List<Word>();

        [System.ComponentModel.DisplayName("Доп. данные")]
        public string Extra { get; set; }

        public List<Word> extras = new List<Word>();

        public void AddToObjects(Word word)
        {
            if (!this.objects.Contains(word))
                this.objects.Add(word);
        }

        public void AddToTrends(Word word)
        {
            if (!this.trends.Contains(word))
                this.trends.Add(word);
        }

        public void AddToExtras(Word word)
        {
            if (!this.extras.Contains(word))
                this.extras.Add(word);
        }

        public ExtractedData()
        {
            Date = Object = Trend = Extra = string.Empty;
        }

        public ExtractedData(string trend)
        {
            this.Trend = trend;
            Object = Date = Extra = string.Empty;
        }

        public ExtractedData(string obj, string trend)
        {
            this.Object = obj;
            this.Trend = trend;
            this.Date = string.Empty;
        }

        public ExtractedData(string obj, string trend, string date)
        {
            this.Object = obj;
            this.Trend = trend;
            this.Date = date;
        }

        public ExtractedData(string obj, string trend, string date, string extra)
        {
            this.Object = obj;
            this.Trend = trend;
            this.Date = date;
            this.Extra = extra;
        }

        public bool isValidWithoutDate()
        {
            return !(this.Object == string.Empty || this.Trend == string.Empty);
        }

        public bool isValid()
        {
            return !(this.Object == string.Empty || this.Trend == string.Empty || this.Date == string.Empty);
        }

        public static List<ExtractedData> Filter(List<ExtractedData> dataList, string filter)
        {
            List<ExtractedData> result = new List<ExtractedData>();
            Regex regex = new Regex(".*" + filter + ".*");
            foreach(ExtractedData data in dataList)
            {
                if (regex.IsMatch(data.Object) || regex.IsMatch(data.Date))
                    result.Add(data);
            }
            if (result.Count == 0)
                return null;
            else
                return result;
        }
    }
}
