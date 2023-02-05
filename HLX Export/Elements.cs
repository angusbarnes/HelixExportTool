using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLXExport
{
    internal static class Elements
    {
        private static Dictionary<string, string> _elements = new Dictionary<string, string>()
        {
            {"Au","Gold"},
            {"AuR","Gold"},
            {"Al","Aluminium"},
            {"Al2O3",""},
            {"As","Asmuth"},
            {"B","Boron"},
            {"Ba","Barium"},
            {"BaO",""},
            {"Be","Beryllium"},
            {"Bi","Bismuth"},
            {"Ca","Calcium"},
            {"CaO","Calcium Oxide"},
            {"Cd","Cadium"},
            {"Ce",""},
            {"Co","Cobalt"},
            {"Cr",""},
            {"Cr2O3",""},
            {"Cs",""},
            {"Cu","Copper"},
            {"Dy",""},
            {"Er",""},
            {"Eu",""},
            {"Fe","Iron"},
            {"Fe2O3","Ferric Oxide"},
            {"Ga",""},
            {"Gd",""},
            {"Ge",""},
            {"Hf",""},
            {"Hg",""},
            {"Ho",""},
            {"In",""},
            {"K","Potassium"},
            {"K2O","Potassium Oxide"},
            {"LOI",""},
            {"LOI1000",""},
            {"La",""},
            {"Li","Lithium"},
            {"Lu",""},
            {"Mg","Magnesium"},
            {"MgO","Magnesium Oxide"},
            {"Mn",""},
            {"MnO",""},
            {"Mo",""},
            {"Na","Sodium"},
            {"Na2O","Sodium Oxide"},
            {"Nb",""},
            {"Nd",""},
            {"Ni",""},
            {"P",""},
            {"P2O5","Phosphorus Pentoxide"},
            {"Pb","Lead"},
            {"Pd",""},
            {"Pd2",""},
            {"Pr",""},
            {"Pt",""},
            {"Pt2",""},
            {"Rb",""},
            {"Re",""},
            {"S","Sulfur"},
            {"SO3","Sulfur Trioxide"},
            {"Sb",""},
            {"Sc",""},
            {"Se",""},
            {"Si",""},
            {"SiO2","Silicon Dioxide"},
            {"Sm",""},
            {"Sn","Zinc"},
            {"Sr",""},
            {"Ta",""},
            {"Tb",""},
            {"Te",""},
            {"Th",""},
            {"Ti","Titanium"},
            {"TiO2",""},
            {"TI",""},
            {"Tm",""},
            {"U",""},
            {"W",""},
            {"Y",""},
            {"Yb",""},
            {"Zn",""},
            {"Zr",""},
        };

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
