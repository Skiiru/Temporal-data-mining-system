using edu.stanford.nlp.pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using mshtml;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.RegularExpressions;
using System.IO;

namespace Temporal_data_mining_system
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<ExtractedData> extractedData;
        List<ExtractedData> filtredData;
        List<ExtractedData> emptyList;
        StanfordCoreNLP pipeline;
        AnnotationPipeline sutimePipeline;
        string text;
        OpenFileDialog ofdTextFile;
        SaveFileDialog sfdResult;
        Dictionary<string, List<ExtractedData>> extractedDataByObject;
        Dictionary<string, List<ExtractedData>> extractedDataByDate;
        double extractingTime;
        int corruptedElements;
        int notExracted;
        double accuracy;

        public MainWindow()
        {
            InitializeComponent();
            extractedData = new List<ExtractedData>();
            emptyList = new List<ExtractedData>();
            text = String.Empty;
            ofdTextFile = new OpenFileDialog();
            sfdResult = new SaveFileDialog();
            extractedDataByObject = new Dictionary<string, List<ExtractedData>>();
            filtredData = new List<ExtractedData>();
            extractedDataByDate = new Dictionary<string, List<ExtractedData>>();
            chart.ChartAreas.Add(new ChartArea("Default"));
            extractingTime = corruptedElements = notExracted = 0;
            accuracy = 100;
        }

        #region Custom functions
        private void ActivateItems()
        {
            //TODO: commands
        }

        private void DeactivateItems()
        {
            treeViewTab.Visibility = Visibility.Hidden;
            chartTab.Visibility = Visibility.Hidden;
            reportAndStatisticsTab.Visibility = Visibility.Hidden;
        }

        private void FillDictionaries()
        {
            treeView.Items.Clear();
            extractedDataByObject.Clear();
            extractedDataByDate.Clear();
            if (extractedData != null && extractedData.Count > 0)
            {
                foreach (ExtractedData data in extractedData)
                {
                    //object
                    if (extractedDataByObject.ContainsKey(data.Object))
                        extractedDataByObject[data.Object].Add(data);
                    else
                    {
                        List<ExtractedData> newList = new List<ExtractedData>();
                        newList.Add(data);
                        extractedDataByObject.Add(data.Object, newList);
                    }
                    //date
                    if (extractedDataByDate.ContainsKey(data.Date))
                        extractedDataByDate[data.Date].Add(data);
                    else
                    {
                        List<ExtractedData> newList = new List<ExtractedData>();
                        newList.Add(data);
                        extractedDataByDate.Add(data.Date, newList);
                    }
                }
            }
            //creating tree tab
            foreach (KeyValuePair<string, List<ExtractedData>> kvp in extractedDataByObject)
            {
                TreeViewItem item = new TreeViewItem();
                string end = kvp.Value.Count == 1 ? " item)" : " items)";
                item.Header = kvp.Key + "(" + kvp.Value.Count + end;
                foreach (ExtractedData data in kvp.Value)
                {
                    item.Items.Add(data.Date + ": " + data.Trend);
                }
                treeView.Items.Add(item);
            }
        }

        private void FilterData(string filter)
        {
            if (filter != string.Empty)
            {
                filtredData = ExtractedData.Filter(extractedData, filter);
                if (filtredData.Count == 0)
                {
                    dgExtractedData.ItemsSource = emptyList;
                }
                else
                    dgExtractedData.ItemsSource = filtredData;
            }
            else
            {
                dgExtractedData.ItemsSource = extractedData;
            }
        }

        private void loadPipelinesAndAnylizeText()
        {
            extractingTime = corruptedElements = notExracted = 0;
            imageLoading.Visibility = Visibility.Visible;
            DateTime now = DateTime.UtcNow;
            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 0));
            if (pipeline == null || sutimePipeline == null)
                Task<List<ExtractedData>>.Factory.StartNew(() =>
                {
                    try
                    {
                        sutimePipeline = TemporalDataExtractor.GetTemporalPipeline();
                        pipeline = TemporalDataExtractor.GetPipeline();
                        TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                        List<ExtractedData> result = temporalExtractor.parse(text, pipeline, sutimePipeline);
                        return result;
                    }
                    catch
                    {
                        return null;
                    }
                }
                ).ContinueWith(list =>
                {
                    extractingTime = (DateTime.UtcNow - now).TotalSeconds;
                    if (list.Result != null)
                    {
                        extractedData = list.Result;
                        dgExtractedData.ItemsSource = extractedData;
                    }
                    else
                        MessageBox.Show("Error in loading language models or creating pipelines.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (extractedData != null && extractedData.Count > 0)
                    {
                        foreach (ExtractedData data in extractedData)
                            if (string.IsNullOrEmpty(data.Object) || string.IsNullOrEmpty(data.Trend) || string.IsNullOrEmpty(data.Date))
                                corruptedElements++;
                        lbExtractedElements.Content = extractedData.Count;
                        lbExtractingTime.Content = extractingTime;
                        lbCorruptedElements.Content = corruptedElements;
                        treeViewTab.Visibility = Visibility.Visible;
                        chartTab.Visibility = Visibility.Visible;
                        reportAndStatisticsTab.Visibility = Visibility.Visible;
                        FillDictionaries();
                    }
                    imageLoading.Visibility = Visibility.Hidden;
                    accuracy = 100 * ((double)(extractedData.Count - corruptedElements) / (double)extractedData.Count);
                    accuracy = Math.Round(accuracy, 2);
                    lbAccuracy.Content = "Total: " + accuracy + "%";
                }, TaskScheduler.FromCurrentSynchronizationContext());
            else
            {
                try
                {
                    extractingTime = corruptedElements = notExracted = 0;
                    Task<List<ExtractedData>>.Factory.StartNew(() =>
                    {
                        try
                        {
                            TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                            List<ExtractedData> result = temporalExtractor.parse(text, pipeline, sutimePipeline);
                            return result;
                        }
                        catch
                        {
                            return null;
                        }
                    }).ContinueWith(list =>
                    {
                        extractingTime = (DateTime.UtcNow - now).TotalSeconds;
                        if (list.Result != null)
                        {

                            extractedData = list.Result;
                            dgExtractedData.ItemsSource = extractedData;
                        }
                        if (extractedData != null && extractedData.Count > 0)
                        {
                            foreach (ExtractedData data in extractedData)
                                if (string.IsNullOrEmpty(data.Object) || string.IsNullOrEmpty(data.Trend) || string.IsNullOrEmpty(data.Date))
                                    corruptedElements++;
                            lbExtractedElements.Content = extractedData.Count;
                            lbExtractingTime.Content = extractingTime;
                            lbCorruptedElements.Content = corruptedElements;
                            treeViewTab.Visibility = Visibility.Visible;
                            chartTab.Visibility = Visibility.Visible;
                            reportAndStatisticsTab.Visibility = Visibility.Visible;
                            FillDictionaries();
                        }
                        imageLoading.Visibility = Visibility.Hidden;
                        accuracy = 100 * ((double)(extractedData.Count - corruptedElements) / (double)extractedData.Count);
                        accuracy = Math.Round(accuracy, 2);
                        lbAccuracy.Content = "Total: " + accuracy + "%";
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    imageLoading.Visibility = Visibility.Hidden;
                }
            }
        }

        private void DrawObjectsChart()
        {
            if (extractedDataByObject != null && extractedDataByObject.Count > 0)
            {

                // Добавим линию, и назначим ее в ранее созданную область "Default"
                Series series = new Series("Count");
                series.ChartArea = "Default";
                series.ChartType = SeriesChartType.Column;

                // добавим данные линии
                List<String> axisXData = new List<String>();
                List<double> axisYData = new List<double>();
                series.Points.DataBindXY(axisXData, axisYData);

                foreach (KeyValuePair<string, List<ExtractedData>> kvp in extractedDataByObject)
                {
                    axisXData.Add(kvp.Key);
                    axisYData.Add(kvp.Value.Count);

                }
                series.Points.DataBindXY(axisXData, axisYData);
                chart.Series.Add(series);
            }
        }

        private void DrawDatesChart()
        {
            if (extractedDataByDate != null && extractedDataByDate.Count > 0)
            {

                // Добавим линию, и назначим ее в ранее созданную область "Default"
                Series series = new Series("Count");
                series.ChartArea = "Default";
                series.ChartType = SeriesChartType.Column;

                // добавим данные линии
                List<String> axisXData = new List<String>();
                List<double> axisYData = new List<double>();
                series.Points.DataBindXY(axisXData, axisYData);

                foreach (KeyValuePair<string, List<ExtractedData>> kvp in extractedDataByDate)
                {
                    axisXData.Add(kvp.Key);
                    axisYData.Add(kvp.Value.Count);

                }
                series.Points.DataBindXY(axisXData, axisYData);
                chart.Series.Add(series);
            }
        }

        private MemoryStream GetObjectsChart()
        {
            if (extractedDataByObject != null && extractedDataByObject.Count > 0)
            {
                MemoryStream memoryStream = new MemoryStream();
                chart.Series.Clear();
                DrawObjectsChart();
                chart.SaveImage(memoryStream, ChartImageFormat.Png);
                cbGraphicType.SelectedIndex = 0;
                return memoryStream;
            }
            else
            {
                cbGraphicType.SelectedIndex = 0;
                return null;
            }
        }

        private MemoryStream GetDatesChart()
        {
            if (extractedDataByDate != null && extractedDataByDate.Count > 0)
            {
                MemoryStream memoryStream = new MemoryStream();
                chart.Series.Clear();
                DrawDatesChart();
                chart.SaveImage(memoryStream, ChartImageFormat.Png);
                cbGraphicType.SelectedIndex = 0;
                return memoryStream;
            }
            else
            {
                cbGraphicType.SelectedIndex = 0;
                return null;
            }
        }

        private void openFile()
        {
            if ((bool)ofdTextFile.ShowDialog())
            {
                DeactivateItems();
                text = FileManager.ReadFile(ofdTextFile.FileName);
                tbInputText.Text = text;
                imageLoading.Visibility = Visibility.Visible;
                loadPipelinesAndAnylizeText();
                ActivateItems();
            }
        }
        #endregion

        #region Events
        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void bOpenFile_Click(object sender, RoutedEventArgs e)
        {
            openFile();
        }

        private void menuSaveToJSON_Click(object sender, RoutedEventArgs e)
        {
            sfdResult.Filter = "JSON|*.json";
            sfdResult.ShowDialog();
            if (sfdResult.FileName != string.Empty)
            {
                if (filtredData != null && filtredData.Count > 0)
                {
                    FileManager.saveToJSON(sfdResult.FileName, filtredData);
                }
                else if (extractedData != null && extractedData.Count > 0)
                {
                    FileManager.saveToJSON(sfdResult.FileName, extractedData);
                }
                else
                    MessageBox.Show("Nothing to save!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                MessageBox.Show("Path is empty", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);

        }

        private void menuSaveToXML_Click(object sender, RoutedEventArgs e)
        {
            sfdResult.Filter = "XML|*.xml";
            sfdResult.ShowDialog();
            if (sfdResult.FileName != string.Empty)
            {
                if (filtredData != null && filtredData.Count > 0)
                {
                    FileManager.saveToXML(sfdResult.FileName, filtredData);
                }
                else if (extractedData != null && extractedData.Count > 0)
                {
                    FileManager.saveToXML(sfdResult.FileName, extractedData);
                }
                else
                    MessageBox.Show("Nothing to save!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                MessageBox.Show("Path is empty", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void tbFilter_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && tbFilter.Text != string.Empty)
            {
                FilterData(tbFilter.Text);
            }
        }

        private void menuSaveToCSV_Click(object sender, RoutedEventArgs e)
        {
            sfdResult.Filter = "CSV|*.csv";
            sfdResult.ShowDialog();
            if (sfdResult.FileName != string.Empty)
            {
                if (filtredData != null && filtredData.Count > 0)
                {
                    FileManager.saveToCSV(sfdResult.FileName, filtredData);
                }
                else if (extractedData != null && extractedData.Count > 0)
                {
                    FileManager.saveToCSV(sfdResult.FileName, extractedData);
                }
                else
                    MessageBox.Show("Nothing to save!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                MessageBox.Show("Path is empty", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void tbURL_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (Regex.IsMatch(tbURL.Text, @"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$"))
                    browser.Source = new Uri(tbURL.Text);
                else
                    browser.Source = new Uri(@"https://www.google.ru/webhp#newwindow=0&q=" + tbURL.Text);
            }
        }

        private void bAnalyzePage_Click(object sender, RoutedEventArgs e)
        {
            DeactivateItems();
            try
            {
                var doc = browser.Document as HTMLDocument;
                if (doc != null)
                {
                    var currentSelection = doc.selection;
                    if (currentSelection != null)
                    {
                        var selectionRange = currentSelection.createRange();
                        if (selectionRange != null)
                        {
                            if (selectionRange.Text != null)
                                text = selectionRange.Text;
                            else
                                text = doc.body.innerText;
                            text = text.Replace(System.Environment.NewLine, string.Empty);
                            tbInputText.Text = text;
                        }
                    }
                }
                loadPipelinesAndAnylizeText();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ActivateItems();
        }

        private void cbGraphicType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //chart is null when this event handles for first time
            if (chart != null)
            {
                ComboBoxItem selected = ((sender as ComboBox).SelectedItem as ComboBoxItem);
                //Not switch cause it's not support dynamic cases (names) and custom classes
                if (selected == cbiNone)
                {
                    chart.Series.Clear();
                }
                else if (selected == cbiObjects)
                {
                    chart.Series.Clear();
                    DrawObjectsChart();
                }
                else if (selected == cbiDates)
                {
                    chart.Series.Clear();
                    DrawDatesChart();
                }
            }
        }

        private void tbNotExtracted_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (extractedData != null)
                if (!(string.IsNullOrEmpty(tbNotExtracted.Text) && string.IsNullOrWhiteSpace(tbNotExtracted.Text)))
                {
                    try
                    {
                        notExracted = Convert.ToInt32(tbNotExtracted.Text);
                        accuracy = 100 * ((double)(extractedData.Count - corruptedElements) / (double)(notExracted + extractedData.Count));
                        accuracy = Math.Round(accuracy, 2);
                        lbAccuracy.Content = "Total: " + accuracy + "%";

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    accuracy = (extractedData.Count - corruptedElements) / extractedData.Count;
                    lbAccuracy.Content = "Total: " + accuracy + "%";
                }
        }

        private void bSaveReport_Click(object sender, RoutedEventArgs e)
        {
            MemoryStream objectChart = null;
            if ((bool)cbObjectChart.IsChecked)
                objectChart = GetObjectsChart();
            MemoryStream dateChart = null;
            if ((bool)cbDateChart.IsChecked)
                dateChart = GetDatesChart();
            List<String> statistics = null;
            if ((bool)cbStatistick.IsChecked)
            {
                statistics = new List<string>();
                statistics.Add("Extracting time: " + extractingTime);
                statistics.Add("Extracted elements: " + extractedData.Count);
                statistics.Add("Corrupted elements: " + corruptedElements);
                statistics.Add("Not extracted: " + notExracted);
                statistics.Add("Accuracy: " + accuracy + "%");
            }
            if (cbReportFormat.SelectedItem == cbiReportDOCX)
            {
                sfdResult.Filter = "MS Word|*.docx";
                sfdResult.ShowDialog();
                string path = sfdResult.FileName;
                FileManager.SaveReportWord(text, extractedData, path, objectChart, dateChart, statistics);
            }
            else if (cbReportFormat.SelectedItem == cbiReportPDF)
            {
                sfdResult.Filter = "PDF|*.pdf";
                sfdResult.ShowDialog();
                string path = sfdResult.FileName;
                FileManager.SaveReportPDF(text, extractedData, path, objectChart, dateChart, statistics);
            }
            else
                MessageBox.Show("Plese select extension!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuSaveToPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MemoryStream objectChart = GetObjectsChart();
                MemoryStream dateChart = GetDatesChart();
                List<String> statistics = new List<string>();
                statistics.Add("Extracting time: " + extractingTime);
                statistics.Add("Extracted elements: " + extractedData.Count);
                statistics.Add("Corrupted elements: " + corruptedElements);
                statistics.Add("Not extracted: " + notExracted);
                statistics.Add("Accuracy: " + accuracy + "%");
                sfdResult.Filter = "PDF|*.pdf";
                sfdResult.ShowDialog();
                string path = sfdResult.FileName;
                FileManager.SaveReportPDF(text, extractedData, path, objectChart, dateChart, statistics);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuSaveToWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MemoryStream objectChart = GetObjectsChart();
                MemoryStream dateChart = GetDatesChart();
                List<String> statistics = new List<string>();
                statistics.Add("Extracting time: " + extractingTime);
                statistics.Add("Extracted elements: " + extractedData.Count);
                statistics.Add("Corrupted elements: " + corruptedElements);
                statistics.Add("Not extracted: " + notExracted);
                statistics.Add("Accuracy: " + accuracy + "%");
                sfdResult.Filter = "MS Word|*.docx";
                sfdResult.ShowDialog();
                string path = sfdResult.FileName;
                FileManager.SaveReportWord(text, extractedData, path, objectChart, dateChart, statistics);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void bClearFilter_Click(object sender, RoutedEventArgs e)
        {
            filtredData.Clear();
            dgExtractedData.ItemsSource = extractedData;
        }


        private void menuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            openFile();
        }

        #endregion
    }
}
