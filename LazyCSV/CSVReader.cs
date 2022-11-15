using System.Collections;
using System.Text;

namespace LazyCSV
{
    public class CSVReader : IDisposable
    {
        private readonly FileStream _stream;
        private readonly TextReader _reader;

        private readonly Dictionary<string, int> _columnHeaders;

        public CSVReader(string path, bool fileContainsHeaders = true)
        {
            FileStreamOptions options = new FileStreamOptions()
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.SequentialScan
            };

            _stream = new FileStream(path, options);
            _reader = new StreamReader(_stream);

            _columnHeaders = new Dictionary<string, int>();

            if (fileContainsHeaders) {
                var row = ReadRow();
                for (int i = 0; i < row.Length; i++) {
                    _columnHeaders[row[i]] = i;
                }
            }
        }

        public string[] GetFieldNames { get { return _columnHeaders.Keys.ToArray(); } }

        public CSVRow ReadRow()
        {
            if (IsEOF)
                throw new Exception("CSV reader cannot read past EOF. Did you check IsEOF before attempting to read?");
            
            string? row = _reader.ReadLine();

            // Potentially Cursed circular reference in _columnHeaders??
            return new CSVRow(row.Split(','), _columnHeaders);
        }

        public bool IsEOF { get { return _stream.Position == _stream.Length; } }

        public void Dispose()
        {
            _stream.Dispose();
            _reader.Dispose();
        }
    }

    public struct CSVRow : IEnumerable<string>
    {
        private readonly string[] _row;
        private readonly Dictionary<string, int> _fieldnames;

        public CSVRow(string[] row, Dictionary<string, int> fieldnames)
        {
            _row = row;
            _fieldnames = fieldnames;
        }

        public CSVRow(List<string> row, Dictionary<string, int> fieldnames)
        {
            _row = row.ToArray();
            _fieldnames = fieldnames;
        }

        public int Length {  get { return _row.Length; } }

        public string GetValue(string fieldname)
        {
            if (_fieldnames.ContainsKey(fieldname) == false)
                throw new System.Exception("CSV does not contain the field requested: " + fieldname);

            return _row[_fieldnames[fieldname]];
        }

        public string GetIndex(int index)
        {
            return _row[index];
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)_row).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _row.GetEnumerator();
        }

        public string this[int index] {
            get { return _row[index]; }
        }
    }
}