using System;
using System.Collections.Generic;
using java.util;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.time;
using edu.stanford.nlp.util;
using edu.stanford.nlp.ling;

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
            SentenceText = string.Empty;
            SentenceWithPOS = string.Empty;
            Dates = new List<TimeStamp>();
            Words = new List<Word>();
        }

        public bool HaveCC()
        {
            return Words.Find(w => w.EdgeRelation == "CC") != null;
        }

        public void NormilizeText()
        {
            //this.NormilizedSentenceText = Regex.Replace(this.SentenceText, @"[^a-zA-Z0-9]", " ");
            bool afterDigit = false;
            for (int i = 0; i < SentenceText.Length; ++i)
            {
                if (Char.IsLetterOrDigit(SentenceText[i]) || Char.IsWhiteSpace(SentenceText[i]))
                {
                    NormilizedSentenceText += SentenceText[i];
                    afterDigit = Char.IsDigit(SentenceText[i]);
                }
                else if (afterDigit && (new List<char> { '.', ':', '/' }).Contains(SentenceText[i]))
                    NormilizedSentenceText += SentenceText[i];
                else if (new List<char> { '%', '$', '€', '\\' }.Contains(SentenceText[i]))
                    NormilizedSentenceText += ' ' + Char.ConvertFromUtf32(SentenceText[i]) + ' ';
                else if (SentenceText[i] == '\'')
                    NormilizedSentenceText += @" '";
                else
                    NormilizedSentenceText += ' ';
            }
            NormilizedSentenceText.Trim();
            NormilizedSentenceText = NormilizedSentenceText.Remove(NormilizedSentenceText.Length - 1);
        }

        public void AddToWords(Word word)
        {
            if (!Words.Contains(word))
                Words.Add(word);
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
            var sentenceWords = NormilizedSentenceText.Split(' ');
            foreach (Word word in Words)
            {
                word.Index = NormilizedSentenceText.IndexOf(word.word);
            }
            int ccIndex = int.MaxValue;
            bool addToLastItem = false;
            foreach (string word in sentenceWords)
            {
                if (!(string.IsNullOrEmpty(word) || string.IsNullOrWhiteSpace(word)))
                {
                    Word currentWord = Words.Find(w => w.word == word);
                    TimeStamp currentPartOfDate = Dates.Find(d => d.Index == currentWord.Index);
                    if (currentWord.EdgeRelation == "cc")
                    {
                        ccIndex = currentWord.Index;
                        addToLastItem = false;
                    }
                    for (int dataIndex = 0; dataIndex < ExtractedTemporalData.Count; ++dataIndex)
                    {
                        if (currentPartOfDate == null)
                        {
                            bool lastIndex = dataIndex == ExtractedTemporalData.Count - 1;
                            for (int i = 0; i < ExtractedTemporalData[dataIndex].objects.Count; ++i)
                            {
                                if (word == ExtractedTemporalData[dataIndex].objects[i].word)
                                {
                                    if (ExtractedTemporalData[dataIndex].objects[i].Index > ccIndex)
                                    {
                                        if (addToLastItem)
                                        {
                                            if (!lastIndex)
                                            {
                                                ExtractedTemporalData[dataIndex + 1].objects.AddRange(ExtractedTemporalData[dataIndex].objects.GetRange(i, ExtractedTemporalData[dataIndex].objects.Count - i));
                                                ExtractedTemporalData[dataIndex].objects.RemoveRange(i, ExtractedTemporalData[dataIndex].objects.Count - i);
                                            }
                                            else
                                                ExtractedTemporalData[dataIndex].Object += ExtractedTemporalData[dataIndex].objects[i].word + ' ';
                                        }
                                        else
                                        {
                                            ExtractedData newData = new ExtractedData();
                                            newData.objects.AddRange(ExtractedTemporalData[dataIndex].objects.GetRange(i, ExtractedTemporalData[dataIndex].objects.Count - i));
                                            ExtractedTemporalData[dataIndex].objects.RemoveRange(i, ExtractedTemporalData[dataIndex].objects.Count - i);
                                            ExtractedTemporalData.Add(newData);
                                            addToLastItem = true;
                                        }
                                    }
                                    else
                                        ExtractedTemporalData[dataIndex].Object += ExtractedTemporalData[dataIndex].objects[i].word + ' ';
                                }
                            }
                            for (int i = 0; i < ExtractedTemporalData[dataIndex].trends.Count; ++i)
                            {
                                if (word == ExtractedTemporalData[dataIndex].trends[i].word)
                                    if (ExtractedTemporalData[dataIndex].trends[i].Index > ccIndex)
                                    {
                                        if (addToLastItem)
                                        {
                                            if (!lastIndex)
                                            {
                                                ExtractedTemporalData[dataIndex + 1].trends.AddRange(ExtractedTemporalData[dataIndex].trends.GetRange(i, ExtractedTemporalData[dataIndex].trends.Count - i));
                                                ExtractedTemporalData[dataIndex].trends.RemoveRange(i, ExtractedTemporalData[dataIndex].trends.Count - i);
                                            }
                                            else
                                                ExtractedTemporalData[dataIndex].Trend += ExtractedTemporalData[dataIndex].trends[i].word + ' ';
                                        }
                                        else
                                        {
                                            ExtractedData newData = new ExtractedData();
                                            newData.trends.AddRange(ExtractedTemporalData[dataIndex].trends.GetRange(i, ExtractedTemporalData[dataIndex].trends.Count - i));
                                            ExtractedTemporalData[dataIndex].trends.RemoveRange(i, ExtractedTemporalData[dataIndex].trends.Count - i);
                                            ExtractedTemporalData.Add(newData);
                                            addToLastItem = true;
                                        }
                                    }
                                    else
                                        ExtractedTemporalData[dataIndex].Trend += ExtractedTemporalData[dataIndex].trends[i].word + ' ';
                            }
                            for (int i = 0; i < ExtractedTemporalData[dataIndex].extras.Count; ++i)
                            {
                                if (word == ExtractedTemporalData[dataIndex].extras[i].word)
                                    if (ExtractedTemporalData[dataIndex].extras[i].Index > ccIndex)
                                    {
                                        if (addToLastItem)
                                        {
                                            if (!lastIndex)
                                            {
                                                ExtractedTemporalData[dataIndex + 1].extras.AddRange(ExtractedTemporalData[dataIndex].extras.GetRange(i, ExtractedTemporalData[dataIndex].extras.Count - i));
                                                ExtractedTemporalData[dataIndex].extras.RemoveRange(i, ExtractedTemporalData[dataIndex].extras.Count - i);
                                            }
                                            else
                                                ExtractedTemporalData[dataIndex].Extra += ExtractedTemporalData[dataIndex].extras[i].word + ' ';
                                        }
                                        else
                                        {
                                            ExtractedData newData = new ExtractedData();
                                            newData.extras.AddRange(ExtractedTemporalData[dataIndex].extras.GetRange(i, ExtractedTemporalData[dataIndex].extras.Count - i));
                                            ExtractedTemporalData[dataIndex].extras.RemoveRange(i, ExtractedTemporalData[dataIndex].extras.Count - i);
                                            ExtractedTemporalData.Add(newData);
                                            addToLastItem = true;
                                        }
                                    }
                                    else
                                        ExtractedTemporalData[dataIndex].Extra += ExtractedTemporalData[dataIndex].extras[i].word + ' ';
                            }
                        }
                        else
                            if (ExtractedTemporalData[dataIndex].Date == "")
                            ExtractedTemporalData[dataIndex].Date = currentPartOfDate.Type;
                    }
                }
            }

            FillTemporalData();
        }

        private void FillTemporalData()
        {
            if (ExtractedTemporalData.Count > 1)
            {
                for (int i = 1; i < ExtractedTemporalData.Count; ++i)
                {
                    if (ExtractedTemporalData[i].Object == string.Empty)
                        ExtractedTemporalData[i].Object = ExtractedTemporalData[i - 1].Object;
                    if (ExtractedTemporalData[i].Trend == string.Empty)
                        ExtractedTemporalData[i].Trend = ExtractedTemporalData[i - 1].Trend;
                    if (ExtractedTemporalData[i].Date == string.Empty)
                        ExtractedTemporalData[i].Date = ExtractedTemporalData[i - 1].Date;
                }

                for (int i = ExtractedTemporalData.Count - 1; i >= 0; --i)
                {
                    if (ExtractedTemporalData[i].Object == string.Empty)
                        ExtractedTemporalData[i].Object = ExtractedTemporalData[i + 1].Object;
                    if (ExtractedTemporalData[i].Trend == string.Empty)
                        ExtractedTemporalData[i].Trend = ExtractedTemporalData[i + 1].Trend;
                    if (ExtractedTemporalData[i].Date == string.Empty)
                        ExtractedTemporalData[i].Date = ExtractedTemporalData[i + 1].Date;
                }
            }
        }
    }
}
