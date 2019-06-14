using System;

namespace Example {
    public class Data {
        public string     ID         { get; set; }
        public DataObject Object     { get; set; }
        public string     Entry      { get; set; }
        public DateTime   CreateDate { get; set; }
        public DateTime   UpdateDate { get; set; }
        public string     Status     { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class DataObject {
        public string ID   { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}