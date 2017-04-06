using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TYCSpider.Model
{

    public class CompanyBasicInfo
    {
        public string Id { get; set; }

        public string CompanyName { get; set; }
        public string TYCCompanyUrl { get; set; }

        public string LegalPersonName { get; set; }

        public string RegisterMoney { get; set; }

        public string RegisterDate { get; set; }

        public string Status { get; set; }

        public string CompanyType { get; set; }

        public string Address { get; set; }

        public string Scope { get; set; }

        public string OperationPeriod { get; set; }

    }
}
