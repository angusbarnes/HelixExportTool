using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLXExport
{
    public class DisplayTable : INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
}
