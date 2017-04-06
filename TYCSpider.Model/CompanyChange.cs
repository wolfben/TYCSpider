using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TYCSpider.Model
{
    public class CompanyChange
    {
        public string CompanyName { get; set; }

        public string ChangeDate { get; set; }

        public string ChangeProject { get; set; }

        public string ChangeBefore { get; set; }

        public string ChangeAfter { get; set; }
    }
}
