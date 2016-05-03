using edu.stanford.nlp.pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Win32;
using System.Windows.Controls;
using mshtml;

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
        Dictionary<string, List<ExtractedData>> extractedDataForTree;

        public MainWindow()
        {
            InitializeComponent();
            extractedData = new List<ExtractedData>();
            text = String.Empty;
            ofdTextFile = new OpenFileDialog();
            sfdResult = new SaveFileDialog();
            extractedDataForTree = new Dictionary<string, List<ExtractedData>>();
            filtredData = new List<ExtractedData>();
        }

        #region Custom functions
        private void ActivateItems()
        {
            //TODO: commands
        }

        private void DeactivateItems()
        {
            this.treeViewTab.Visibility = Visibility.Hidden;
        }

        private void FillTree()
        {
            this.treeView.Items.Clear();
            this.extractedDataForTree.Clear();
            if (this.extractedData != null && this.extractedData.Count > 0)
            {
                foreach (ExtractedData data in this.extractedData)
                {
                    if (this.extractedDataForTree.ContainsKey(data.Object))
                        this.extractedDataForTree[data.Object].Add(data);
                    else
                    {
                        List<ExtractedData> newList = new List<ExtractedData>();
                        newList.Add(data);
                        this.extractedDataForTree.Add(data.Object, newList);
                    }
                }
            }
            foreach (KeyValuePair<string, List<ExtractedData>> kvp in this.extractedDataForTree)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = kvp.Key;
                foreach (ExtractedData data in kvp.Value)
                {
                    item.Items.Add(data.Date + ": " + data.Trend);
                }
                this.treeView.Items.Add(item);
            }
        }

        private void FilterData(string filter)
        {
            if (filter != string.Empty)
            {
                this.filtredData = ExtractedData.Filter(this.extractedData, filter);
                if (this.filtredData != null)
                {
                    this.dgExtractedData.Items.Clear();
                    foreach (ExtractedData data in this.filtredData)
                    {
                        this.dgExtractedData.Items.Add(data);
                    }
                }
            }
            else
            {
                this.dgExtractedData.Items.Clear();
                foreach (ExtractedData data in this.extractedData)
                {
                    this.dgExtractedData.Items.Add(data);
                }
            }
        }

        private void loadPipelines()
        {
            this.imageLoading.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke((Action)(() => this.tabControl.SelectedIndex = 0));
            if (this.pipeline == null || this.sutimePipeline == null)
                Task<Boolean>.Factory.StartNew(() =>
                {
                    try
                    {
                        this.sutimePipeline = Sentence.GetTemporalPipeline();
                        this.pipeline = TemporalDataExtractor.GetPipeline();
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
                        this.dgExtractedData.ItemsSource = extractedData;
                    }
                    else
                        MessageBox.Show("Error in loading language models or creating pipelines.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (this.extractedData != null && this.extractedData.Count > 0)
                    {
                        this.treeViewTab.Visibility = Visibility.Visible;
                        FillTree();
                    }
                    this.imageLoading.Visibility = Visibility.Hidden;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            else
            {
                try
                {
                    TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                    extractedData = temporalExtractor.parse(text, pipeline, sutimePipeline);
                    this.dgExtractedData.ItemsSource = extractedData;
                    if (this.extractedData != null && this.extractedData.Count > 0)
                    {
                        this.treeViewTab.Visibility = Visibility.Visible;
                        FillTree();
                    }
                    this.imageLoading.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.imageLoading.Visibility = Visibility.Hidden;
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
            this.tbInputText.Text = text;
            this.imageLoading.Visibility = Visibility.Visible;
            loadPipelines();
            this.imageLoading.Visibility = Visibility.Hidden;
            ActivateItems();
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }

        private void menuSaveToJSON_Click(object sender, RoutedEventArgs e)
        {
            this.sfdResult.Filter = "JSON|*.json";
            this.sfdResult.ShowDialog();
            List<ExtractedData> dataForSave = new List<ExtractedData>();
            foreach(ExtractedData data in this.dgExtractedData.Items)
            {
                dataForSave.Add(data);
            }
            if (dataForSave != null && dataForSave.Count > 0 && this.sfdResult.FileName != string.Empty)
            {
                FileManager.saveToJSON(sfdResult.FileName, dataForSave);
            }
        }

        private void menuSaveToXML_Click(object sender, RoutedEventArgs e)
        {
            this.sfdResult.Filter = "XML|*.xml";
            this.sfdResult.ShowDialog();
            List<ExtractedData> dataForSave = new List<ExtractedData>();
            foreach (ExtractedData data in this.dgExtractedData.Items)
            {
                dataForSave.Add(data);
            }
            if (dataForSave != null && dataForSave.Count > 0 && this.sfdResult.FileName!=string.Empty)
            {
                FileManager.saveToXML(sfdResult.FileName, dataForSave);
            }
        }

        private void tbFilter_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter && this.tbFilter.Text !=string.Empty)
            {
                FilterData(this.tbFilter.Text);
            }
        }

        private void menuSaveToCSV_Click(object sender, RoutedEventArgs e)
        {
            this.sfdResult.Filter = "CSV|*.csv";
            this.sfdResult.ShowDialog();
            List<ExtractedData> dataForSave = new List<ExtractedData>();
            foreach (ExtractedData data in this.dgExtractedData.Items)
            {
                dataForSave.Add(data);
            }
            if (dataForSave != null && dataForSave.Count > 0 && this.sfdResult.FileName != string.Empty)
            {
                FileManager.saveToXML(sfdResult.FileName, dataForSave);
            }
        }
        #endregion

        private void tbURL_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter)
            {
                this.browser.Source = new Uri(this.tbURL.Text);
            }
        }

        private void bAnalyzePage_Click(object sender, RoutedEventArgs e)
        {
            DeactivateItems();
            try
            {
                var doc = this.browser.Document as HTMLDocument;
                if (doc != null)
                {
                    var currentSelection = doc.selection;
                    if (currentSelection != null)
                    {
                        var selectionRange = currentSelection.createRange();
                        if (selectionRange != null)
                        {
                            this.text = selectionRange.Text;
                            this.tbInputText.Text = this.text;
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
    }
}
