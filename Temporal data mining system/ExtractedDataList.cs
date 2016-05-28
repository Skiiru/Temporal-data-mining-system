using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Temporal_data_mining_system
{
    public class ExtractedDataList
    {
        List<ExtractedData> extractedData;

        public ExtractedDataList(List<ExtractedData> data)
        {
            this.extractedData = data;
        }
        public ExtractedDataList() { }
    }
}
