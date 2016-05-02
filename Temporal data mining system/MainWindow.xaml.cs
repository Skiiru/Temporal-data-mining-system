using edu.stanford.nlp.pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Win32;
using System.Windows.Controls;

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
            this.treeView.Items.Clear();
            //TODO: commands
        }

        private void DeactivateItems()
        {
            this.treeViewTab.Visibility = Visibility.Hidden;
        }

        private void FillTreeDictionary()
        {
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
            this.imageLoading.Visibility = Visibility.Visible;
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
                        text = FileManager.ReadFile(ofdTextFile.FileName);
                        this.tbInputText.Text = text;
                        TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                        extractedData = temporalExtractor.parse(text, pipeline, sutimePipeline);
                        this.dgExtractedData.Items.Clear();
                        foreach (ExtractedData data in extractedData)
                        {
                            this.dgExtractedData.Items.Add(data);
                        }
                    }
                    else
                        MessageBox.Show("Error in loading language models or creating pipelines.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.imageLoading.Visibility = Visibility.Hidden;
                    if (this.extractedData != null && this.extractedData.Count > 0)
                    {
                        this.treeViewTab.Visibility = Visibility.Visible;
                        FillTreeDictionary();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            else
            {
                try
                {
                    text = FileManager.ReadFile(ofdTextFile.FileName);
                    this.tbInputText.Text = text;
                    TemporalDataExtractor temporalExtractor = new TemporalDataExtractor();
                    extractedData = temporalExtractor.parse(text, pipeline, sutimePipeline);
                    this.dgExtractedData.Items.Clear();
                    foreach (ExtractedData data in extractedData)
                    {
                        this.dgExtractedData.Items.Add(data);
                    }
                    if (this.extractedData != null && this.extractedData.Count > 0)
                    {
                        this.treeViewTab.Visibility = Visibility.Visible;
                        FillTreeDictionary();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                this.imageLoading.Visibility = Visibility.Hidden;
            }
            ActivateItems();
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }
        #endregion

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
    }
}
