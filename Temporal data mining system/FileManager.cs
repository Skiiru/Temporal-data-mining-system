using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Serialization;
using MSWord = Microsoft.Office.Interop.Word;
using System.Reflection;
using System;

namespace Temporal_data_mining_system
{
    class FileManager
    {

        public static string ReadFile(string filePath)
        {
            var file = filePath.Split('.');
            switch(file[file.Length - 1].ToLower())
            {
                case "txt":
                    return openTxtFile(filePath);
                case "docx":
                    return openWordFile(filePath);
                default:
                    throw new IOException("Wrong file format.");
            }
        }

        public static void saveToXML(string path, List<ExtractedData> dataList)
        {
            using (StreamWriter stream = new StreamWriter(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ExtractedData>));
                serializer.Serialize(stream, dataList);
            }
        }

        public static void saveToJSON(string path, List<ExtractedData> dataList)
        {
            string json = JsonConvert.SerializeObject(dataList);
            File.WriteAllText(path, json);
        }

        private static string openWordFile(string path)
        {
            try
            {
                //using interfaces
                MSWord._Application application;
                MSWord._Document document;

                application = new MSWord.Application();
                Object file = @path;
                document = application.Documents.Open(ref file);
                MSWord.Range textRange = document.Content;
                return textRange.Text;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string openTxtFile(string path)
        {
            string result = string.Empty;
            try
            {
                result = File.ReadAllText(path);
            }
            catch { }
            return result;
        }
    }
}
