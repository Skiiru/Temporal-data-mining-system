using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Serialization;
using MSWord = Microsoft.Office.Interop.Word;
using CsvHelper;
using System;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;
using iTextSharp.text;

namespace Temporal_data_mining_system
{
    class FileManager
    {

        public static string ReadFile(string filePath)
        {
            var file = filePath.Split('.');
            switch (file[file.Length - 1].ToLower())
            {
                case "txt":
                    return openTxtFile(filePath);
                case "docx":
                    return openWordFile(filePath);
                case "pdf":
                    return openPDFFile(filePath);
                default:
                    throw new IOException("Wrong file format.");
            }
        }

        public static void saveToXML(string path, List<ExtractedData> dataList)
        {
            using (StreamWriter stream = new StreamWriter(path))
            {
                ExtractedDataList list = new ExtractedDataList(dataList);
                XmlSerializer serializer = new XmlSerializer(typeof(ExtractedDataList));
                serializer.Serialize(stream, list);
            }
        }

        public static void saveToJSON(string path, List<ExtractedData> dataList)
        {
            string json = JsonConvert.SerializeObject(dataList);
            File.WriteAllText(path, json);
        }

        public static void saveToCSV(string path, List<ExtractedData> datalist)
        {
            using (StreamWriter sw = new StreamWriter(path))
            using (CsvWriter csvWriter = new CsvWriter(sw))
                csvWriter.WriteRecords(datalist);
        }

        public static void SaveReportPDF(string text, List<ExtractedData> dataList, string path, MemoryStream objectChart = null, MemoryStream dateChart = null, List<String> statistics = null)
        {
            var doc = new Document();
            PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
            doc.Open();

            doc.Add(new Phrase("Text: " + Environment.NewLine + text, new Font(Font.FontFamily.TIMES_ROMAN, 14, Font.NORMAL, BaseColor.BLACK)));

            //Table
            int columnCount = 4;
            PdfPTable table = new PdfPTable(columnCount);

            //Header
            PdfPCell cell = new PdfPCell(new Phrase("Extracted data", new Font(Font.FontFamily.TIMES_ROMAN, 16, Font.NORMAL, BaseColor.BLACK)));
            cell.BackgroundColor = BaseColor.WHITE;
            cell.Padding = 5;
            cell.Colspan = 4;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
            table.AddCell("Date");
            table.AddCell("Object");
            table.AddCell("Trend");
            table.AddCell("Additional inf");

            //Body
            foreach (ExtractedData data in dataList)
            {
                //Date
                cell = new PdfPCell(new Phrase(data.Date, new Font(Font.FontFamily.TIMES_ROMAN, 14, Font.NORMAL, BaseColor.BLACK)));
                table.AddCell(cell);
                //Object
                cell.Phrase = new Phrase(data.Object, new Font(Font.FontFamily.TIMES_ROMAN, 14, Font.NORMAL, BaseColor.BLACK));
                table.AddCell(cell);
                //Trend
                cell.Phrase = new Phrase(data.Trend, new Font(Font.FontFamily.TIMES_ROMAN, 14, Font.NORMAL, BaseColor.BLACK));
                table.AddCell(cell);
                //Addtitional information
                cell.Phrase = new Phrase(data.Extra, new Font(Font.FontFamily.TIMES_ROMAN, 14, Font.NORMAL, BaseColor.BLACK));
                table.AddCell(cell);
            }
            doc.Add(table);
            doc.Add(new Phrase(Environment.NewLine));

            //Charts
            if (objectChart != null)
            {
                doc.Add(new Phrase("Objects chart" + Environment.NewLine, new Font(Font.FontFamily.TIMES_ROMAN, 16, Font.NORMAL, BaseColor.BLACK)));
                Image img = Image.GetInstance(objectChart.GetBuffer());
                img.ScalePercent(75f);
                doc.Add(img);
                doc.Add(new Phrase(Environment.NewLine));
            }
            if (dateChart != null)
            {
                doc.Add(new Phrase("Dates chart" + Environment.NewLine, new Font(Font.FontFamily.TIMES_ROMAN, 16, Font.NORMAL, BaseColor.BLACK)));
                Image img = Image.GetInstance(dateChart.GetBuffer());
                img.ScalePercent(75f);
                doc.Add(img);
                doc.Add(new Phrase(Environment.NewLine));
            }

            if (statistics != null)
            {
                string statText = "Statistics: " + Environment.NewLine;
                statistics.ForEach(str => statText += str + Environment.NewLine);
                doc.Add(new Phrase(statText, new Font(Font.FontFamily.TIMES_ROMAN, 14, Font.NORMAL, BaseColor.BLACK)));
            }

            doc.Close();
        }

        public static void SaveReportWord(string text, List<ExtractedData> dataList, string path, MemoryStream objectChart = null, MemoryStream dateChart = null, List<String> statistics = null)
        {
            try
            {
                object oMissing = System.Reflection.Missing.Value;
                object oEndOfDoc = "\\endofdoc"; /* \endofdoc is a predefined bookmark */
                int columnCount = 4;

                //Start Word and create a new document.
                MSWord._Application oWord;
                MSWord._Document oDoc;
                oWord = new MSWord.Application();
                oDoc = oWord.Documents.Add(ref oMissing, ref oMissing, ref oMissing, ref oMissing);

                //Text
                MSWord.Paragraph oParaText;
                oParaText = oDoc.Content.Paragraphs.Add(ref oMissing);
                oParaText.Range.Text = "Text: " + Environment.NewLine + text;
                oParaText.Range.Font.Size = 14;
                oParaText.Format.SpaceAfter = 24;    //24 pt spacing after paragraph.
                oParaText.Range.InsertParagraphAfter();

                //Insert a title at the beginning of the document.
                MSWord.Paragraph oParaTitle;
                oParaTitle = oDoc.Content.Paragraphs.Add(ref oMissing);
                oParaTitle.Range.Text = "Extracted data";
                oParaTitle.Range.Font.Size = 16;
                oParaTitle.Range.Font.Bold = 1;
                oParaTitle.Format.SpaceAfter = 24;    //24 pt spacing after paragraph.
                oParaTitle.Range.InsertParagraphAfter();

                //Insert a 3 x 5 table, fill it with data, and make the first row
                //bold and italic.
                MSWord.Table oTable;
                MSWord.Range wrdRng = oDoc.Bookmarks.get_Item(ref oEndOfDoc).Range;
                oTable = oDoc.Tables.Add(wrdRng, dataList.Count + 1, columnCount, ref oMissing, ref oMissing);
                oTable.Range.ParagraphFormat.SpaceAfter = 6;
                oTable.Borders.InsideLineStyle = MSWord.WdLineStyle.wdLineStyleSingle;
                oTable.Borders.OutsideLineStyle = MSWord.WdLineStyle.wdLineStyleSingle;


                oTable.Cell(1, 1).Range.Text = "Date";
                oTable.Cell(1, 2).Range.Text = "Object";
                oTable.Cell(1, 3).Range.Text = "Trend";
                oTable.Cell(1, 4).Range.Text = "Additional inf.";
                for (int r = 2; r < dataList.Count + 1; r++)
                {
                    oTable.Cell(r, 1).Range.Text = dataList[r - 1].Date;
                    oTable.Cell(r, 2).Range.Text = dataList[r - 1].Object;
                    oTable.Cell(r, 3).Range.Text = dataList[r - 1].Trend;
                    oTable.Cell(r, 4).Range.Text = dataList[r - 1].Extra;
                }

                if (objectChart != null)
                {
                    //Insert a title
                    MSWord.Paragraph oParaTableObjectTitle;
                    oParaTableObjectTitle = oDoc.Content.Paragraphs.Add(ref oMissing);
                    oParaTableObjectTitle.Range.Text = Environment.NewLine + "Objects chart";
                    oParaTableObjectTitle.Range.Font.Size = 16;
                    oParaTableObjectTitle.Range.Font.Bold = 1;
                    oParaTableObjectTitle.Format.SpaceAfter = 24;    //24 pt spacing after paragraph.
                    oParaTableObjectTitle.Range.InsertParagraphAfter();

                    System.Drawing.Image img = System.Drawing.Image.FromStream(objectChart);
                    System.Windows.Clipboard.SetDataObject(img);
                    MSWord.Paragraph oParaObjectPict = oDoc.Content.Paragraphs.Add(ref oMissing);
                    oParaObjectPict.Range.Paste();
                    oParaObjectPict.Range.InsertParagraphAfter();
                }

                if (dateChart != null)
                {
                    //Insert a title
                    MSWord.Paragraph oParaTableObjectTitle;
                    oParaTableObjectTitle = oDoc.Content.Paragraphs.Add(ref oMissing);
                    oParaTableObjectTitle.Range.Text = Environment.NewLine + "Dates chart";
                    oParaTableObjectTitle.Range.Font.Size = 16;
                    oParaTableObjectTitle.Range.Font.Bold = 1;
                    oParaTableObjectTitle.Format.SpaceAfter = 24;    //24 pt spacing after paragraph.
                    oParaTableObjectTitle.Range.InsertParagraphAfter();

                    System.Drawing.Image img = System.Drawing.Image.FromStream(dateChart);
                    System.Windows.Clipboard.SetDataObject(img);
                    MSWord.Paragraph oParaObjectPict = oDoc.Content.Paragraphs.Add(ref oMissing);
                    oParaObjectPict.Range.Paste();
                    oParaObjectPict.Range.InsertParagraphAfter();
                }


                if (statistics != null)
                {
                    MSWord.Paragraph oParaStat;
                    oParaStat = oDoc.Content.Paragraphs.Add(ref oMissing);
                    string statText = "Statistics: " + Environment.NewLine;
                    statistics.ForEach(str => statText += str + Environment.NewLine);
                    oParaStat.Range.Text = statText;
                    oParaStat.Range.Font.Size = 14;
                    oParaStat.Format.SpaceAfter = 24;    //24 pt spacing after paragraph.
                    oParaStat.Range.InsertParagraphAfter();
                }
                oDoc.SaveAs2(path);
                oWord.Quit();
            }
            catch { }
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

        private static string openPDFFile(string path)
        {
            StringBuilder text = new StringBuilder();

            if (File.Exists(path))
            {
                PdfReader pdfReader = new PdfReader(path);

                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

                    currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                    text.Append(currentText);
                }
                pdfReader.Close();
            }
            return text.ToString();
        }
    }
}
