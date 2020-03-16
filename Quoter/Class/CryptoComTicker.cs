using System.Collections.Generic;

namespace Quoter.Class
{
    public class Ticker
    {
        public string symbol { get; set; }
        public string high { get; set; }
        public string vol { get; set; }
        public string last { get; set; }
        public string low { get; set; }
        public string buy { get; set; }
        public string sell { get; set; }
        public string change { get; set; }
        public string rose { get; set; }
    }

    public class Data
    {
        public long date { get; set; }
        public List<Ticker> ticker { get; set; }
    }

    public class CryptoComTicker
    {
        public string code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }
    }
}
