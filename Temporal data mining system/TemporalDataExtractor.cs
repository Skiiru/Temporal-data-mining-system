using System;
using System.Collections.Generic;
using System.Linq;
using java.util;
using java.io;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.time;
using edu.stanford.nlp.trees;
using edu.stanford.nlp.semgraph;
using System.Text.RegularExpressions;
using edu.stanford.nlp.dcoref;
using System.IO;

namespace Temporal_data_mining_system
{
    class TemporalDataExtractor
    {
        /// <summary>
        /// Создает аннотатор для текста
        /// </summary>
        /// <returns></returns>
        public static StanfordCoreNLP GetPipeline()
        {
            // Path to the folder with models extracted from `stanford-corenlp-3.5.2-models.jar`
            var jarRoot = Environment.CurrentDirectory + @"\models";

            // Annotation pipeline configuration
            var props = new java.util.Properties();
            props.setProperty("annotators", "tokenize, ssplit, pos, lemma, ner, parse, dcoref"); //tokenize, ssplit, pos, lemma, ner, parse, dcoref
            props.setProperty("ner.useSUTime", "0");

            // We should change current directory, so StanfordCoreNLP could find all the model files automatically
            var curDir = Environment.CurrentDirectory;
            Directory.SetCurrentDirectory(jarRoot);
            var pipeline = new StanfordCoreNLP(props);
            Directory.SetCurrentDirectory(curDir);
            return pipeline;
        }

        /// <summary>
        /// Анализирует текст и разбивает его на предложения
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="pipeline">Аннотатор для текста</param>
        /// <param name="sutimePipeline">Аннотатор для предлодения</param>
        /// <returns></returns>
        public List<ExtractedData> parse(string text, StanfordCoreNLP pipeline, AnnotationPipeline sutimePipeline)
        {
            if (text != null)
            {
                // Annotation
                var annotation = new Annotation(text);
                pipeline.annotate(annotation);

                var sentences = annotation.get(typeof(CoreAnnotations.SentencesAnnotation));
                string res = string.Empty;

                List<ExtractedData> lstOfData = new List<ExtractedData>();
                foreach (Annotation sentence in sentences as ArrayList)
                {
                    List<string> date = new List<string>();
                    //Извлечение данных
                    Sentence s = Sentence.GetTemporal(sentence.toString(), sutimePipeline);
                    if (s != null)
                    {
                        SemanticGraph dependencies = sentence.get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation)) as SemanticGraph;
                        s.SentenceWithPOS = dependencies.toEnUncollapsedSentenceString();
                        s.ExtractedTemporalData = ParseSemanticGraph(ref s, dependencies, s.Dates);
                        s.PrepareValues();
                        lstOfData.AddRange(s.ExtractedTemporalData);
                    }

                    //var documentCoref = annotation.get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
                }

                return lstOfData;
            }
            else
                return null;

        }

        public List<ExtractedData> ParseSemanticGraph(ref Sentence sentence, SemanticGraph dependencies, List<TimeStamp> dates)
        {
            /* The IndexedWord object is very similar to the CoreLabel object 
                only is used in the SemanticGraph context */
            IndexedWord firstRoot = dependencies.getFirstRoot();
            var outEdges = dependencies.getOutEdgesSorted(firstRoot);
            List<ExtractedData> extractedData = new List<ExtractedData>();
            ExtractedData data = new ExtractedData();
            Word firstRootWord = new Word(firstRoot.originalText(), firstRoot.tag(), "ROOT", sentence.SentenceText.IndexOf(firstRoot.originalText()));
            sentence.AddToWords(firstRootWord);
            data.AddToTrends(firstRootWord);
            bool needed = true;
            for (int i = 0; i < outEdges.size(); ++i)
            {
                SemanticGraphEdge edge = (SemanticGraphEdge)outEdges.get(i);
                IndexedWord iWord = edge.getDependent();
                var relation = edge.getRelation().getShortName();
                Word word = new Word(iWord.originalText(), iWord.tag(), relation, sentence.SentenceText.IndexOf(iWord.originalText()));
                sentence.AddToWords(word);
                word.FillExtractedData(ref data, sentence, needed);
                ParseEdges(ref sentence, outEdges.get(i) as SemanticGraphEdge, ref data, dependencies, dates, needed);
            }
            extractedData.Add(data);
            return extractedData;
        }

        private void ParseEdges(ref Sentence sentence, SemanticGraphEdge edge, ref ExtractedData data, SemanticGraph dependencies, List<TimeStamp> dates, bool needed)
        {
            IndexedWord vertex = edge.getDependent();
            string relation = edge.getRelation().getShortName();
            Word word = new Word(vertex.originalText(), vertex.tag(), relation, sentence.SentenceText.IndexOf(vertex.originalText()));
            sentence.AddToWords(word);
            needed = word.FillExtractedData(ref data, sentence, needed);
            var outEdges = dependencies.getOutEdgesSorted(vertex);
            for (int i = 0; i < outEdges.size(); ++i)
            {
                ParseEdges(ref sentence, outEdges.get(i) as SemanticGraphEdge, ref data, dependencies, dates, needed);
            }
        }


        /// <summary>
        /// Анализирует предложение и выделяет в нем темпоральные данные
        /// </summary>
        /// <param name="inputSentence">Предложение</param>
        /// <returns>Список найденных темпоральных данных в виде класса Data</returns>
        //private List<ExtractedData> parseSentence(Sentence inputSentence)
        //{
        //    string sentence = inputSentence.SentenceWithPOS;
        //    string[] splited = sentence.Split(' ');
        //    string mainNoun, objectNoun, verb;
        //    List<Tuple<string, string, int>> extraNoun = new List<Tuple<string, string, int>>();
        //    List<Tuple<string, string, int>> extraVerb = new List<Tuple<string, string, int>>();
        //    mainNoun = objectNoun = verb = string.Empty;

        //    List<ExtractedData> result = new List<ExtractedData>();
        //    int CCcount = 0;

        //    foreach (string word in splited)
        //    {
        //        //1st element - word, 2nd - part of speach
        //        string[] wordAndPOS = word.Split('/');
        //        short posIndex = 1;
        //        short wordIndex = 0;

        //        bool isPartOfDate = false;

        //        foreach (TimeStamp date in inputSentence.Dates)
        //        {
        //            Regex regex = new Regex(wordAndPOS[wordIndex], RegexOptions.IgnoreCase);
        //            Match match = regex.Match(date);
        //            isPartOfDate = match.Success;
        //            if (isPartOfDate)
        //                break;
        //        }

        //        if (wordAndPOS.Length == 2 && !isPartOfDate)
        //        {
        //            string lastNN = string.Empty;
        //            switch (wordAndPOS[posIndex])
        //            {
        //                case "NNS":
        //                    if (objectNoun == string.Empty)
        //                        objectNoun = wordAndPOS[wordIndex];
        //                    else
        //                        if (CCcount == 0)
        //                        objectNoun += " " + wordAndPOS[wordIndex];
        //                    else
        //                        extraNoun.Add(new Tuple<string, string, int>(wordAndPOS[wordIndex], wordAndPOS[posIndex], CCcount));
        //                    break;
        //                case "NNP":
        //                    if (mainNoun == string.Empty)
        //                        mainNoun = wordAndPOS[wordIndex];
        //                    else
        //                        if (CCcount == 0)
        //                        mainNoun += " " + wordAndPOS[wordIndex];
        //                    else
        //                        extraNoun.Add(new Tuple<string, string, int>(wordAndPOS[wordIndex], wordAndPOS[posIndex], CCcount));
        //                    break;
        //                case "NN":
        //                case "PRP":
        //                    //TODO: вынести основной объект
        //                    lastNN = wordAndPOS[wordIndex];
        //                    if (CCcount != 0)
        //                        extraNoun.Add(new Tuple<string, string, int>(wordAndPOS[wordIndex], wordAndPOS[posIndex], CCcount));//mainNoun += " " + wordAndPOS[wordIndex];
        //                    else
        //                        if (objectNoun == string.Empty)
        //                        objectNoun = wordAndPOS[wordIndex];
        //                    else
        //                        objectNoun += " " + wordAndPOS[wordIndex];
        //                    break;
        //                case "VB":
        //                case "VBN":
        //                case "VBD":
        //                case "VBG":
        //                case "VBP":
        //                    if (CCcount == 0)
        //                        if (verb != string.Empty)
        //                            verb += " " + wordAndPOS[wordIndex];
        //                        else
        //                            verb = wordAndPOS[wordIndex];
        //                    else
        //                        extraVerb.Add(new Tuple<string, string, int>(wordAndPOS[wordIndex], wordAndPOS[posIndex], CCcount));
        //                    break;
        //                case "JJR":
        //                    if (CCcount == 0)
        //                        if (verb != string.Empty)
        //                            verb += " " + wordAndPOS[wordIndex];
        //                        else
        //                            verb = wordAndPOS[wordIndex];
        //                    else
        //                        extraVerb.Add(new Tuple<string, string, int>(wordAndPOS[wordIndex], wordAndPOS[posIndex], CCcount));
        //                    break;
        //                case "RB":
        //                    if (CCcount == 0)
        //                        verb += " " + wordAndPOS[wordIndex];
        //                    else
        //                        extraVerb.Add(new Tuple<string, string, int>(wordAndPOS[wordIndex], wordAndPOS[posIndex], CCcount));
        //                    break;
        //                case "CC":
        //                    CCcount++;
        //                    break;
        //            }
        //        }
        //    }

        //    //Добавление в список данных
        //    string allNoun = mainNoun + " " + objectNoun;

        //    //Обработка дополнительных данных
        //    string lastNoun = allNoun;
        //    string lastVerb = verb;
        //    for (int i = 1; i < inputSentence.Dates.Count; ++i)
        //    {
        //        string eNoun = string.Empty;
        //        string eVerb = string.Empty;
        //        foreach (var tuple in extraNoun)
        //        {
        //            if (tuple.Item3 == i)
        //                if (eNoun == string.Empty)
        //                    eNoun = tuple.Item1;
        //                else
        //                    eNoun += " " + tuple.Item1;
        //        }
        //        foreach (var tuple in extraVerb)
        //        {
        //            if (tuple.Item3 == i)
        //                if (eVerb == string.Empty)
        //                    eVerb = tuple.Item1;
        //                else
        //                    eVerb += " " + tuple.Item1;
        //        }
        //        if (eNoun == string.Empty)
        //            eNoun = lastNoun;
        //        lastNoun = eNoun;
        //        if (eVerb == string.Empty)
        //            eVerb = lastVerb;
        //        lastVerb = eVerb;
        //        if (verb == string.Empty)
        //            verb = eVerb;
        //        result.Add(new ExtractedData(eNoun, eVerb, inputSentence.Dates[i].Type));
        //    }
        //    result.Add(new ExtractedData(allNoun, verb, inputSentence.Dates[0].Type));
        //    return result;
        //}
    }
}
