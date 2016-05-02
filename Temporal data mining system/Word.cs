using System;

namespace Temporal_data_mining_system
{
    class Word
    {
        public string word { get; set; }
        public string POS { get; set; }
        public string EdgeRelation { get; set; }
        public int Index { get; set; }

        public Word(string word, string POS, string relation, int index)
        {
            this.word = word;
            this.POS = POS;
            this.EdgeRelation = relation;
        }

        public bool IsNeeded()
        {
            switch (this.POS)
            {
                case "NNS":
                case "NNP":
                case "NN":
                case "PRP":
                case "VB":
                case "VBN":
                case "VBD":
                case "VBG":
                case "VBP":
                case "JJR":
                case "RB":
                    return true;
                default:
                    return false;

            }
        }

        public bool FillExtractedData(ref ExtractedData data, Sentence sentence, bool needed)
        {
            foreach (TimeStamp date in sentence.Dates)
            {
                if (date.OriginalText.IndexOf(this.word) != -1 && String.IsNullOrEmpty(data.Date))
                {
                    data.Date = date.Type;
                }
            }
            if (needed)
                return SwitchWordRelations(data);
            else
                return false;
        }

        private bool SwitchWordRelations(ExtractedData data)
        {
            switch (this.EdgeRelation)
            {
                case "subj":
                case "nsubj":
                case "nsubjpass":
                case "compound":
                    if (this.POS != "DT")
                    {
                        data.AddToObjects(this);
                        return true;
                    }
                    break;
                case "ROOT":
                case "aux":
                case "auxpass":
                case "cop":
                case "xcomp":
                case "acomp":
                case "ccomp":
                case "advcl":
                case "advmod":
                case "conj":
                    if (this.POS != "NNS" && this.POS != "NN" && this.POS != "NNP" && this.POS != "PRP")
                    {
                        data.AddToTrends(this);
                        return true;
                    }
                    else
                    {
                        data.AddToObjects(this);
                        return true;
                    }
                case "dobj":
                case "iobj":
                case "num":
                case "nummod":
                case "nmod":
                case "case":
                    if (this.POS != "IN")
                    {
                        data.AddToExtras(this);
                        return true;
                    }
                    else
                    {
                        if (this.POS == "NNS" && this.POS == "NN" && this.POS == "NNP" && this.POS == "PRP")
                            data.AddToObjects(this);
                        return true;
                    }
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            return this.word == (obj as Word).word;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

//switch (this.POS)
//{
//    case "NN":
//    case "NNS":
//        if (String.IsNullOrEmpty(data.Extra))
//            data.Extra = this.word;
//        else
//            if (data.Extra.IndexOf(this.word) == -1)
//            data.Extra += ' ' + this.word;
//        break;
//    case "NNP":
//    case "PRP":
//        data.Object += ' ' + this.word;
//        break;
//    case "VB":
//    case "VBN":
//    case "VBD":
//    case "VBG":
//    case "VBP":
//        //Realations by verb means that it's a new part of sentence
//        if (String.IsNullOrEmpty(data.Trend))
//            data.Trend = this.word + ' ';
//        else
//            return new ExtractedData(this.word + ' ');
//        break;
//    case "JJR":
//    case "RB":
//        if (data.Trend.IndexOf(this.word) == -1)
//            data.Trend += this.word + ' ';
//        break;
//}