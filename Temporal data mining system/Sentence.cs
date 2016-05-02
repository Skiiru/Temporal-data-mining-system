using System;
using System.Collections.Generic;
using java.util;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.tagger.maxent;
using edu.stanford.nlp.time;
using edu.stanford.nlp.util;
using edu.stanford.nlp.ling;
using System.Text.RegularExpressions;

namespace Temporal_data_mining_system
{
    class Sentence
    {
        public string SentenceText;
        public string SentenceWithPOS;
        public List<TimeStamp> Dates;
        public List<Word> Words;
        public List<ExtractedData> ExtractedTemporalData;
        private string NormilizedSentenceText = string.Empty;

        public Sentence()
        {
            this.SentenceText = string.Empty;
            this.SentenceWithPOS = string.Empty;
            this.Dates = new List<TimeStamp>();
            this.Words = new List<Word>();
        }

        public bool HaveCC()
        {
            return this.Words.Find(w => w.EdgeRelation == "CC") != null;
        }

        public void NormilizeText()
        {
            //this.NormilizedSentenceText = Regex.Replace(this.SentenceText, @"[^a-zA-Z0-9]", " ");
            bool afterDigit = false;
            for (int i = 0; i < this.SentenceText.Length; ++i)
            {
                if (Char.IsLetterOrDigit(this.SentenceText[i]) || Char.IsWhiteSpace(this.SentenceText[i]))
                {
                    this.NormilizedSentenceText += this.SentenceText[i];
                    afterDigit = Char.IsDigit(this.SentenceText[i]);
                }
                else if (afterDigit && (new List<char> { '.', ':', '/' }).Contains(this.SentenceText[i]))
                    this.NormilizedSentenceText += this.SentenceText[i];
                else if (new List<char> { '%', '$', '€', '\\' }.Contains(this.SentenceText[i]))
                    this.NormilizedSentenceText += ' ' + Char.ConvertFromUtf32(this.SentenceText[i]) + ' ';
                else if (this.SentenceText[i] == '\'')
                    this.NormilizedSentenceText += @" '";
                else
                    this.NormilizedSentenceText += ' ';
            }
            this.NormilizedSentenceText.Trim();
            this.NormilizedSentenceText = this.NormilizedSentenceText.Remove(this.NormilizedSentenceText.Length - 1);
        }

        public void AddToWords(Word word)
        {
            if (!this.Words.Contains(word))
                this.Words.Add(word);
        }

        /// <summary>
        /// Создает источник информации для нахождения темпоральных данных
        /// </summary>
        /// <returns>Источник информации</returns>
        public static AnnotationPipeline GetTemporalPipeline()
        {
            // Path to the folder with models extracted from `stanford-corenlp-3.5.2-models.jar`
            var jarRoot = Environment.CurrentDirectory + @"\models";
            var modelsDirectory = jarRoot + @"\edu\stanford\nlp\models";

            // Annotation pipeline configuration
            var pipeline = new AnnotationPipeline();
            pipeline.addAnnotator(new TokenizerAnnotator(false));
            pipeline.addAnnotator(new WordsToSentencesAnnotator(false));


            // Loading POS Tagger and including them into pipeline
            var tagger = new MaxentTagger(modelsDirectory +
                         @"\pos-tagger\english-bidirectional\english-bidirectional-distsim.tagger");
            pipeline.addAnnotator(new POSTaggerAnnotator(tagger));

            // SUTime configuration
            var sutimeRules = modelsDirectory + @"\sutime\defs.sutime.txt,"
                              + modelsDirectory + @"\sutime\english.holidays.sutime.txt,"
                              + modelsDirectory + @"\sutime\english.sutime.txt";
            var props = new java.util.Properties();
            props.setProperty("sutime.rules", sutimeRules);
            props.setProperty("sutime.binders", "0");
            props.setProperty("sutime.markTimeRanges", "true");
            props.setProperty("sutime.includeRange", "true");
            pipeline.addAnnotator(new TimeAnnotator("sutime", props));

            return pipeline;
        }

        /// <summary>
        /// Извлекает темпоральные данные из предложения и возвращает их
        /// </summary>
        /// <param name="text">Предложение</param>
        /// <param name="pipeline">Источник информации</param>
        /// <returns></returns>
        public static Sentence GetTemporal(string text, AnnotationPipeline pipeline)
        {
            var annotation = new Annotation(text);
            string curDate = DateTime.Now.ToString("yyyy-MM-dd");
            annotation.set(new CoreAnnotations.DocDateAnnotation().getClass(), curDate);
            pipeline.annotate(annotation);

            string sentence = annotation.get(new CoreAnnotations.TextAnnotation().getClass()) as string;

            var timexAnnsAll = annotation.get(new TimeAnnotations.TimexAnnotations().getClass()) as ArrayList;

            if (!timexAnnsAll.isEmpty())
            {
                Sentence s = new Sentence();
                s.SentenceWithPOS = sentence;
                s.SentenceText = text;
                s.NormilizeText();
                foreach (CoreMap cm in timexAnnsAll)
                {
                    var tokens = cm.get(new CoreAnnotations.TokensAnnotation().getClass()) as List;
                    var time = cm.get(new TimeExpression.Annotation().getClass()) as TimeExpression;
                    var type = time.getTemporal().toString();
                    string originalText = time.getText();
                    s.Dates.Add(new TimeStamp(type, originalText, s.NormilizedSentenceText.IndexOf(originalText)));
                }
                return s;
            }
            else
                return null;
        }

        public void PrepareValues()
        {
            var sentenceWords = this.NormilizedSentenceText.Split(' ');
            foreach (Word word in this.Words)
            {
                word.Index = this.NormilizedSentenceText.IndexOf(word.word);
            }
            int ccIndex = int.MaxValue;
            bool addToLastItem = false;
            foreach (string word in sentenceWords)
            {
                if (!(string.IsNullOrEmpty(word) || string.IsNullOrWhiteSpace(word)))
                {
                    Word currentWord = this.Words.Find(w => w.word == word);
                    TimeStamp currentPartOfDate = this.Dates.Find(d => d.Index == currentWord.Index);
                    if (currentWord.EdgeRelation == "cc")
                    {
                        ccIndex = currentWord.Index;
                        addToLastItem = false;
                    }
                    for (int dataIndex = 0; dataIndex < this.ExtractedTemporalData.Count; ++dataIndex)
                    {
                        if (currentPartOfDate == null)
                        {
                            bool lastIndex = dataIndex == this.ExtractedTemporalData.Count - 1;
                            for (int i = 0; i < this.ExtractedTemporalData[dataIndex].objects.Count; ++i)
                            {
                                if (word == this.ExtractedTemporalData[dataIndex].objects[i].word)
                                {
                                    if (this.ExtractedTemporalData[dataIndex].objects[i].Index > ccIndex)
                                    {
                                        if (addToLastItem)
                                        {
                                            if (!lastIndex)
                                            {
                                                this.ExtractedTemporalData[dataIndex + 1].objects.AddRange(this.ExtractedTemporalData[dataIndex].objects.GetRange(i, this.ExtractedTemporalData[dataIndex].objects.Count - i));
                                                this.ExtractedTemporalData[dataIndex].objects.RemoveRange(i, this.ExtractedTemporalData[dataIndex].objects.Count - i);
                                            }
                                            else
                                                this.ExtractedTemporalData[dataIndex].Object += this.ExtractedTemporalData[dataIndex].objects[i].word + ' ';
                                        }
                                        else
                                        {
                                            ExtractedData newData = new ExtractedData();
                                            newData.objects.AddRange(this.ExtractedTemporalData[dataIndex].objects.GetRange(i, this.ExtractedTemporalData[dataIndex].objects.Count - i));
                                            this.ExtractedTemporalData[dataIndex].objects.RemoveRange(i, this.ExtractedTemporalData[dataIndex].objects.Count - i);
                                            this.ExtractedTemporalData.Add(newData);
                                            addToLastItem = true;
                                        }
                                    }
                                    else
                                        this.ExtractedTemporalData[dataIndex].Object += this.ExtractedTemporalData[dataIndex].objects[i].word + ' ';
                                }
                            }
                            for (int i = 0; i < this.ExtractedTemporalData[dataIndex].trends.Count; ++i)
                            {
                                if (word == this.ExtractedTemporalData[dataIndex].trends[i].word)
                                    if (this.ExtractedTemporalData[dataIndex].trends[i].Index > ccIndex)
                                    {
                                        if (addToLastItem)
                                        {
                                            if (!lastIndex)
                                            {
                                                this.ExtractedTemporalData[dataIndex + 1].trends.AddRange(this.ExtractedTemporalData[dataIndex].trends.GetRange(i, this.ExtractedTemporalData[dataIndex].trends.Count - i));
                                                this.ExtractedTemporalData[dataIndex].trends.RemoveRange(i, this.ExtractedTemporalData[dataIndex].trends.Count - i);
                                            }
                                            else
                                                this.ExtractedTemporalData[dataIndex].Trend += this.ExtractedTemporalData[dataIndex].trends[i].word + ' ';
                                        }
                                        else
                                        {
                                            ExtractedData newData = new ExtractedData();
                                            newData.trends.AddRange(this.ExtractedTemporalData[dataIndex].trends.GetRange(i, this.ExtractedTemporalData[dataIndex].trends.Count - i));
                                            this.ExtractedTemporalData[dataIndex].trends.RemoveRange(i, this.ExtractedTemporalData[dataIndex].trends.Count - i);
                                            this.ExtractedTemporalData.Add(newData);
                                            addToLastItem = true;
                                        }
                                    }
                                    else
                                        this.ExtractedTemporalData[dataIndex].Trend += this.ExtractedTemporalData[dataIndex].trends[i].word + ' ';
                            }
                            for (int i = 0; i < this.ExtractedTemporalData[dataIndex].extras.Count; ++i)
                            {
                                if (word == this.ExtractedTemporalData[dataIndex].extras[i].word)
                                    if (this.ExtractedTemporalData[dataIndex].extras[i].Index > ccIndex)
                                    {
                                        if (addToLastItem)
                                        {
                                            if (!lastIndex)
                                            {
                                                this.ExtractedTemporalData[dataIndex + 1].extras.AddRange(this.ExtractedTemporalData[dataIndex].extras.GetRange(i, this.ExtractedTemporalData[dataIndex].extras.Count - i));
                                                this.ExtractedTemporalData[dataIndex].extras.RemoveRange(i, this.ExtractedTemporalData[dataIndex].extras.Count - i);
                                            }
                                            else
                                                this.ExtractedTemporalData[dataIndex].Extra += this.ExtractedTemporalData[dataIndex].extras[i].word + ' ';
                                        }
                                        else
                                        {
                                            ExtractedData newData = new ExtractedData();
                                            newData.extras.AddRange(this.ExtractedTemporalData[dataIndex].extras.GetRange(i, this.ExtractedTemporalData[dataIndex].extras.Count - i));
                                            this.ExtractedTemporalData[dataIndex].extras.RemoveRange(i, this.ExtractedTemporalData[dataIndex].extras.Count - i);
                                            this.ExtractedTemporalData.Add(newData);
                                            addToLastItem = true;
                                        }
                                    }
                                    else
                                        this.ExtractedTemporalData[dataIndex].Extra += this.ExtractedTemporalData[dataIndex].extras[i].word + ' ';
                            }
                        }
                        else
                            if (this.ExtractedTemporalData[dataIndex].Date == "")
                            this.ExtractedTemporalData[dataIndex].Date = currentPartOfDate.Type;
                    }
                }
            }

            FillTemporalData();
        }

        private void FillTemporalData()
        {
            if (this.ExtractedTemporalData.Count > 1)
            {
                for (int i = 1; i < this.ExtractedTemporalData.Count; ++i)
                {
                    if (this.ExtractedTemporalData[i].Object == string.Empty)
                        this.ExtractedTemporalData[i].Object = this.ExtractedTemporalData[i - 1].Object;
                    if (this.ExtractedTemporalData[i].Trend == string.Empty)
                        this.ExtractedTemporalData[i].Trend = this.ExtractedTemporalData[i - 1].Trend;
                    if (this.ExtractedTemporalData[i].Date == string.Empty)
                        this.ExtractedTemporalData[i].Date = this.ExtractedTemporalData[i - 1].Date;
                }

                for (int i = this.ExtractedTemporalData.Count - 1; i >= 0; --i)
                {
                    if (this.ExtractedTemporalData[i].Object == string.Empty)
                        this.ExtractedTemporalData[i].Object = this.ExtractedTemporalData[i + 1].Object;
                    if (this.ExtractedTemporalData[i].Trend == string.Empty)
                        this.ExtractedTemporalData[i].Trend = this.ExtractedTemporalData[i + 1].Trend;
                    if (this.ExtractedTemporalData[i].Date == string.Empty)
                        this.ExtractedTemporalData[i].Date = this.ExtractedTemporalData[i + 1].Date;
                }
            }
        }
    }
}
