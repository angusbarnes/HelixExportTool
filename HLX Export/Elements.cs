﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLXExport
{
    internal static class Elements
    {
        private static Dictionary<string, string> _elements = new Dictionary<string, string>();

        public static Dictionary<string, string> ExposeDictionary() {
            return _elements;
        }


        public static void Load(Dictionary<string, string> dictToLoad) {
            dictToLoad.ToList().ForEach(x => _elements.Add(x.Key, x.Value));
        }

        public static ElementMatchResult MatchCommonName(string twoLetterCode)
        {
            ElementMatchResult result = _elements.ContainsKey(twoLetterCode);

            if (result)
                result.SetValues(twoLetterCode, _elements[twoLetterCode], true);

            return result;
        }

        public class ElementMatchResult
        {
            private string twoLetterCode;
            private string commonName;
            public bool IsMatch { get; protected set; }

            public ElementMatchResult(string TwoLetterCode, string CommonName, bool result)
            {
                twoLetterCode = TwoLetterCode;
                commonName = CommonName;
                IsMatch = result;
            }

            public void SetValues(string TwoLetterCode, string CommonName, bool result)
            {
                twoLetterCode = TwoLetterCode;
                commonName = CommonName;
                IsMatch = result;
            }

            public ElementMatchResult(bool result)
            {
                twoLetterCode = "";
                commonName = "";
                IsMatch= result;
            }

            public string TwoLetterCode { get { return twoLetterCode; }}
            public string CommonName { get { return commonName; }}

            public static implicit operator bool(ElementMatchResult self)
            {
                return self.IsMatch;
            }

            public static implicit operator ElementMatchResult(bool result)
            {
                return new ElementMatchResult(result);
            }
        }
    }
}
