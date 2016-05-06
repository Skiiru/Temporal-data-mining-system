using edu.stanford.nlp.pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using mshtml;
using System.Windows.Forms.DataVisualization.Charting;

namespace Temporal_data_mining_system
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<ExtractedData> extractedData;
        List<ExtractedData> filtredData;
        StanfordCoreNLP pipeline;
        AnnotationPipeline sutimePipeline;
        string text;
        OpenFileDialog ofdTextFile;
        SaveFileDialog sfdResult;
        Dictionary<string, List<ExtractedData>> extractedDataByObject;
        Dictionary<string, List<ExtractedData>> extractedDataByDate;

        public MainWindow()
        {
            InitializeComponent();
            extractedData = new List<ExtractedData>();
            text = String.Empty;
            ofdTextFile = new OpenFileDialog();
            sfdResult = new SaveFileDialog();
            extractedDataByObject = new Dictionary<string, List<ExtractedData>>();
            filtredData = new List<ExtractedData>();
            extractedDataByDate = new Dictionary<string, List<ExtractedData>>();
        }

        #region Custom functions
        private void ActivateItems()
        {
            //TODO: commands
        }

        private void DeactivateItems()
        {
            treeViewTab.Visibility = Visibility.Hidden;
            graphicTab.Visibility = Visibility.Hidden;
        }

        private void FillDictionaris()
        {
            treeView.Items.Clear();
            extractedDataByObject.Clear();
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
                if (filtredData != null)
                {
                    dgExtractedData.Items.Clear();
                    foreach (ExtractedData data in filtredData)
                    {
                        dgExtractedData.Items.Add(data);
                    }
                }
            }
            else
            {
                dgExtractedData.Items.Clear();
                foreach (ExtractedData data in extractedData)
                {
                    dgExtractedData.Items.Add(data);
                }
            }
        }

        private void loadPipelines()
        {
            imageLoading.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 0));
            if (pipeline == null || sutimePipeline == null)
                Task<Boolean>.Factory.StartNew(() =>
                {
                    try
                    {
                        sutimePipeline = Sentence.GetTemporalPipeline();
                        pipeline = TemporalDataExtractor.GetPipeline();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                ).ContinueWith(flag =>
                {
                    if (flag.Result)
                    {
                        TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                        extractedData = temporalExtractor.parse(text, pipeline, sutimePipeline);
                        dgExtractedData.ItemsSource = extractedData;
                    }
                    else
                        MessageBox.Show("Error in loading language models or creating pipelines.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (extractedData != null && extractedData.Count > 0)
                    {
                        treeViewTab.Visibility = Visibility.Visible;
                        graphicTab.Visibility = Visibility.Visible;
                        FillDictionaris();
                    }
                    imageLoading.Visibility = Visibility.Hidden;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            else
            {
                try
                {
                    TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                    extractedData = temporalExtractor.parse(text, pipeline, sutimePipeline);
                    dgExtractedData.ItemsSource = extractedData;
                    if (extractedData != null && extractedData.Count > 0)
                    {
                        treeViewTab.Visibility = Visibility.Visible;
                        graphicTab.Visibility = Visibility.Visible;
                        FillDictionaris();
                    }
                    imageLoading.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    imageLoading.Visibility = Visibility.Hidden;
                }
            }

        }
        #endregion

        #region Events
        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void menuService_Click(object sender, RoutedEventArgs e)
        {

        }

        private void bOpenFile_Click(object sender, RoutedEventArgs e)
        {
            ofdTextFile.ShowDialog();
            DeactivateItems();
            text = FileManager.ReadFile(ofdTextFile.FileName);
            tbInputText.Text = text;
            imageLoading.Visibility = Visibility.Visible;
            loadPipelines();
            ActivateItems();
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }

        private void menuSaveToJSON_Click(object sender, RoutedEventArgs e)
        {
            sfdResult.Filter = "JSON|*.json";
            sfdResult.ShowDialog();
            List<ExtractedData> dataForSave = new List<ExtractedData>();
            foreach (ExtractedData data in dgExtractedData.Items)
            {
                dataForSave.Add(data);
            }
            if (dataForSave != null && dataForSave.Count > 0 && sfdResult.FileName != string.Empty)
            {
                FileManager.saveToJSON(sfdResult.FileName, dataForSave);
            }
        }

        private void menuSaveToXML_Click(object sender, RoutedEventArgs e)
        {
            sfdResult.Filter = "XML|*.xml";
            sfdResult.ShowDialog();
            List<ExtractedData> dataForSave = new List<ExtractedData>();
            foreach (ExtractedData data in dgExtractedData.Items)
            {
                dataForSave.Add(data);
            }
            if (dataForSave != null && dataForSave.Count > 0 && sfdResult.FileName != string.Empty)
            {
                FileManager.saveToXML(sfdResult.FileName, dataForSave);
            }
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
            List<ExtractedData> dataForSave = new List<ExtractedData>();
            foreach (ExtractedData data in dgExtractedData.Items)
            {
                dataForSave.Add(data);
            }
            if (dataForSave != null && dataForSave.Count > 0 && sfdResult.FileName != string.Empty)
            {
                FileManager.saveToXML(sfdResult.FileName, dataForSave);
            }
        }

        private void tbURL_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                browser.Source = new Uri(tbURL.Text);
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
                            text = selectionRange.Text;
                            tbInputText.Text = text;
                        }
                    }
                }
                loadPipelines();
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
                    if (extractedDataByObject != null && extractedDataByObject.Count > 0)
                    {
                        chart.ChartAreas.Add(new ChartArea("Default"));

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
                else if (selected == cbiDates)
                {
                    chart.Series.Clear();
                    if (extractedDataByDate != null && extractedDataByDate.Count > 0)
                    {
                        chart.ChartAreas.Add(new ChartArea("Default"));

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
            }
        }
        #endregion
    }
}
