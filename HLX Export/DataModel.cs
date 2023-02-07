using LazyCSV;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HLXExport
{
    public class DataManager
    {
        private Dictionary<string, DataModel> models;

        public DataManager()
        {
            models = new Dictionary<string, DataModel>();
        }

        public DataModel GetModel(string filename)
        {
            if (models.ContainsKey(filename))
                return models[filename];

            return new DataModel();
        }

        public bool ContainsModel(string filename)
        {
            return models.ContainsKey(filename);
        }

        public void UpdateDataModelFromDisplay(string filename, IEnumerable<RowDisplayData> rows)
        {
            DataModel model = new DataModel();
            foreach (RowDisplayData row in rows)
            {
                model.Add(row.FieldName, (FieldSettings)row);
            }

            if (models.ContainsKey(filename))
            {
                models[filename] = model;
            } else
            {
                models.Add(filename, model);
            }     

        }

        public void Load(string filename) 
        {
            
            string jsonString = File.ReadAllText(filename);
            models = JsonConvert.DeserializeObject<Dictionary<string, DataModel>>(jsonString);
            Debug.Log(models.FlattenToString());
        }

        public void Save(string filename) 
        {
            foreach (DataModel model in models.Values)
            {
                foreach (FieldSettings item in model.ExposeDictionary().Values)
                {
                    Debug.Log(item.ToString());
                }
            }
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
            string jsonString = JsonConvert.SerializeObject(models, serializerSettings);
            File.WriteAllText(filename, jsonString);
        }
    }

    [Serializable]
    public class DataModel
    {
        public Dictionary<string, FieldSettings> headerSettings;

        public List<string> KeywordBlackList;

        public DataModel()
        {
            headerSettings = new Dictionary<string, FieldSettings>();
            KeywordBlackList = new List<string>();
        }

        public void AddBlacklistedTerm(string term)
        {
            KeywordBlackList.Add(term);
        }

        public void Add(string fieldName, FieldSettings setting)
        {
            headerSettings.Add(fieldName, setting);
        }

        public FieldSettings GetSettings(string fieldName)
        {
            if (headerSettings.ContainsKey(fieldName))
                return headerSettings[fieldName];

            return new FieldSettings() { IncludeField = true, NameMapping = "" };
        }

        public RowDisplayData GetAsRowDisplay(string fieldname)
        {
            RowDisplayData displayData = (RowDisplayData) GetSettings(fieldname);
            displayData.FieldName = fieldname;
            return displayData;
        }

        public Dictionary<string, FieldSettings> ExposeDictionary()
        {
            return headerSettings;
        }
        // Probably Shouldn't be used
        public ObservableCollection<RowDisplayData> AsDataGridTable()
        {
            ObservableCollection<RowDisplayData> table = new ObservableCollection<RowDisplayData>();
            foreach (KeyValuePair<string, FieldSettings> pair in headerSettings)
            {
                table.Add(new RowDisplayData()
                {
                    FieldName = pair.Key,
                    NameMapping = pair.Value.NameMapping,
                    Include = pair.Value.IncludeField
                });
            }

            return table;
        }

        public override string ToString()
        {
            return headerSettings.ToString();
        }
    }

    [Serializable]
    public struct FieldSettings
    {
        public string NameMapping;
        public bool IncludeField;

        public static explicit operator FieldSettings(RowDisplayData row) => new FieldSettings
        {
            NameMapping = row.NameMapping,
            IncludeField = row.Include
        };

        public override string ToString()
        {
            return $"NameMapping={NameMapping}, IncludeField={IncludeField}";
        }
    }

    public class RowDisplayData : INotifyPropertyChanged
    {
        public string FieldName { get; set; }
        public bool Include { get; set; }

        public string NameMapping { get; set; }

        public string DesiredUnit { get; set; }
        public string DetectedUnit { get; set; }
        public string IdentifiedElement { get; set; }
        public string SuggestedName { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static explicit operator RowDisplayData(FieldSettings settings) => new RowDisplayData { 
            NameMapping = settings.NameMapping,
            Include = settings.IncludeField
        };
    }
}
