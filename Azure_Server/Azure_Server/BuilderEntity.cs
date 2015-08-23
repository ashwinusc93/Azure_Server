using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Azure_Server
{
    public class BuilderEntity : TableEntity
    {
        public BuilderEntity(string projectname, string roomnumber)
        {
            this.PartitionKey = projectname;
            this.RowKey = roomnumber;
        }

        public BuilderEntity() { }
        public string buildername { get; set; }
        public string customername { get; set; }
        public string location { get; set; }
        public string contactnumber { get; set; }
    }
}
