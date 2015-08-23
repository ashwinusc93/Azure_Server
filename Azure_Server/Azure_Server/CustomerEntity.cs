using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Azure_Server
{
   public class CustomerEntity:TableEntity
    {
        public CustomerEntity(string customername, string Time)
        {
            this.PartitionKey = customername;
            this.RowKey = Time;
            //this.TimeStamp =Convert.ToDateTime( TimeStamp);
        }

        public CustomerEntity() { }
        public string id { get; set; }
        public string devicefunction { get; set; }
        public string devicename { get; set; }
        public string devicetype { get; set; }
        public string devtabid { get; set; }
        public string ep { get; set; }
        public string vendorname { get; set; }
        public string IEEE { get; set; }
        public string energy1 { get; set; }
        public string energy2 { get; set; }
        public string datetime { get; set; }
    }
}
