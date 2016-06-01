using System.Collections.Generic;
using System.Xml.Serialization;

namespace Temporal_data_mining_system
{
    public class ExtractedDataList
    {
        public List<ExtractedData> extractedData { get; set; }

        public ExtractedDataList(List<ExtractedData> data)
        {
            extractedData = data;
        }

        public ExtractedDataList() { }
    }
}
