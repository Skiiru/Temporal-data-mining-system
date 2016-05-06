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

        public List<Word> objects { get; set; }

        [System.ComponentModel.DisplayName("Тенденция")]
        public string Trend { get; set; }

        public List<Word> trends { get; set; }

        [System.ComponentModel.DisplayName("Доп. данные")]
        public string Extra { get; set; }

        public List<Word> extras { get; set;}

        private void Init()
        {
            extras = new List<Word>();
            trends = new List<Word>();
            objects = new List<Word>();
        }

        public void AddToObjects(Word word)
        {
            if (!objects.Contains(word))
                objects.Add(word);
        }

        public void AddToTrends(Word word)
        {
            if (!trends.Contains(word))
                trends.Add(word);
        }

        public void AddToExtras(Word word)
        {
            if (!extras.Contains(word))
                extras.Add(word);
        }

        public ExtractedData()
        {
            Init();
            Date = Object = Trend = Extra = string.Empty;
        }

        public ExtractedData(string trend)
        {
            Init();
            Trend = trend;
            Object = Date = Extra = string.Empty;
        }

        public ExtractedData(string obj, string trend)
        {
            Object = obj;
            Trend = trend;
            Date = string.Empty;
        }

        public ExtractedData(string obj, string trend, string date)
        {
            Init();
            Object = obj;
            Trend = trend;
            Date = date;
        }

        public ExtractedData(string obj, string trend, string date, string extra)
        {
            Init();
            Object = obj;
            Trend = trend;
            Date = date;
            Extra = extra;
        }

        public bool isValidWithoutDate()
        {
            return !(Object == string.Empty || Trend == string.Empty);
        }

        public bool isValid()
        {
            return !(Object == string.Empty || Trend == string.Empty || Date == string.Empty);
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
