using System.Collections.Generic;

namespace Quoter.Class
{
    public class Datum
    {
        public string symbol { get; set; }
        public string count_coin { get; set; }
        public int amount_precision { get; set; }
        public string base_coin { get; set; }
        public int price_precision { get; set; }
    }

    public class CryptoComSymbols
    {
        public string code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }
    }
}
