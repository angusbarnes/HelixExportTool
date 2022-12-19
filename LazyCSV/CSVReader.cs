using System.Collections;
using System.Text;

namespace LazyCSV
{
    public class CSVReader : IDisposable
    {
        private readonly FileStream _stream;
        private readonly TextReader _reader;

        private readonly Dictionary<string, int> _columnHeaders;

        private bool fileContainsHeaders;

        public int RowCount { get; protected set; }

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

            RowCount = File.ReadLines(path).Count() - (fileContainsHeaders ? 1 : 0);

            if (fileContainsHeaders) {
                var row = ReadRow();
                for (int i = 0; i < row.Length; i++) {
                    _columnHeaders[row[i]] = i;
                }
            }

            this.fileContainsHeaders = fileContainsHeaders;
        }
        /// <summary>
        /// This is a secondary constructor to the CSVReader Class. Using this constructor may lead to a degraded experience
        /// with slower performance and less optimisation on read/write operations
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileContainsHeaders"></param>
        public CSVReader(FileStream stream, bool fileContainsHeaders = true)
        {
            FileStreamOptions options = new FileStreamOptions()
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.SequentialScan
            };
            _stream = stream;
            _reader = new StreamReader(_stream);

            _columnHeaders = new Dictionary<string, int>();

            RowCount = 0;

            while(string.IsNullOrEmpty(_reader.ReadLine()) == false)
            {
                RowCount++;
            }

            RowCount -= fileContainsHeaders ? 1 : 0;

            Reset();

            if (fileContainsHeaders)
            {
                var row = ReadRow();
                for (int i = 0; i < row.Length; i++)
                {
                    _columnHeaders[row[i]] = i;
                }
            }

            this.fileContainsHeaders = fileContainsHeaders;
        }

        public string[] FieldNames { get { return _columnHeaders.Keys.ToArray(); } }

        public CSVRow ReadRow()
        {
            if (IsEOF)
                throw new Exception("CSV reader cannot read past EOF. Did you check IsEOF before attempting to read?");
            
            string? row = _reader.ReadLine();

            // Potentially Cursed circular reference in _columnHeaders??
            return new CSVRow(row.Split(','), _columnHeaders);
        }

        public int CurrentReadPosition { get { return (int) _stream.Position; } }
        public int FileStreamLength { get { return (int) _stream.Length; } }
        public double ReadPercentage {  get { return _stream.Position / _stream.Length; } }
        public string[] GetField(string fieldname) {

            List<string> values = new List<string>();
            while (IsEOF == false) {
                values.Add(ReadRow().GetField(fieldname));
            }

            return values.ToArray();
        }

        public IEnumerable<CSVRow> Read() {
            if (!IsEOF) {
                yield return ReadRow();
            }
        }

        public void Reset() {
            _stream.Position = 0;
            if(fileContainsHeaders)
                _reader.ReadLine();
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

        public string GetField(string fieldname)
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

        public override string ToString() {
            return _row.FlattenToString();
        }

        public string this[int index] {
            get { return _row[index]; }
        }
    }
}