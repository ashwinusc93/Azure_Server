using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.Xml.Serialization;
using System.Xml;
using System.Threading;

namespace Azure_Server
{
    class Program
    {
        Int32 port =8751;
        TcpClient client;
        TcpListener list; public static CloudTable table; public static CloudTableClient tableClient;
        string rd1;
        string Id = null, IEEE = null, Vendorname = null,Energy_off=null,Energy_On=null, DeviceType = null, DeviceName = null, Endpoint = null, customername = null, DeviceFunction = null, DeviceTableID = null, Value=null;

        static void Disable(string interfaceName)
        {
            System.Diagnostics.ProcessStartInfo psi =
                new System.Diagnostics.ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" disable");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();
        }

        static void Enable(string interfaceName)
        {
            System.Diagnostics.ProcessStartInfo psi =
                new System.Diagnostics.ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" enable");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();
        }
        
        public void processfun()
        {
            
            lock (this)
            {
                Enable("Wireless Network Connection");
                list = new TcpListener(IPAddress.Any, port);
                list.Start();
                client = list.AcceptTcpClient();
                StreamReader sr = new StreamReader(client.GetStream());
                rd1 = sr.ReadToEnd().ToString();//read xml data from android device 
                Console.WriteLine("read   " + rd1.Length);
                File.WriteAllText("D:\\dataforCSharp.xml", rd1);//put it in tempoorary file to send this data to azure table.
                list.Stop();
                client.Close();
                Disable("Wireless Network Connection");
                Thread.Sleep(3000);
                if (rd1.ElementAt(0) == '<')
                {
            
                    send_to_table();
                }
                else if (rd1.ElementAt(0) == '@')
                {
                    
                    send_to_builder();
                }
                else if (rd1.ElementAt(0) == '#')
                {
                    retrive_from_builder_customer();
                }
                else if (rd1.ElementAt(0) == '$')
                {
                    send_customer_info_to_builder();
                    }
                else
                    receive_from_table();
               
            }
        }
        public void send_to_table()
        {
             int count = 0;     
            //to send data to the azure table.
            using (XmlReader reader = XmlReader.Create("D:\\dataforCSharp.xml"))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        //return only when you have START tag
                        switch (reader.Name)
                        {
                            case "col":
                                // Detect this article element.
                                //Console.WriteLine("Start <col> element.");
                                // Search for the attribute name on this current node.
                                string attribute = reader["name"];
                                // Next read will contain text.
                                if (reader.Read())
                                {
                                    if (attribute == "ieee")
                                        IEEE = reader.Value.Trim();
                                    else if (attribute == "devicefunction")
                                        DeviceFunction = reader.Value.Trim();
                                    else if (attribute == "devicetype")
                                        DeviceType = reader.Value.Trim();
                                    else if (attribute == "devicename")
                                        DeviceName = reader.Value.Trim();
                                    else if (attribute == "devtabid")
                                        DeviceTableID = reader.Value.Trim();
                                    else if (attribute == "ep")
                                        Endpoint = reader.Value.Trim();
                                    else if (attribute == "vendorname")
                                        Vendorname = reader.Value.Trim();
                                        else if (attribute=="energy1")
                                        Energy_off = reader.Value.Trim();
                                    else if (attribute=="energy2")
                                        Energy_On = reader.Value.Trim();
                                    else if (attribute == "customername")
                                    {
                                        customername = reader.Value.Trim();
                                        if (count == 0)
                                        {
                                            var date = DateTime.Now;
                                            table = tableClient.GetTableReference(customername + date.Hour);
                                            try
                                            {
                                                if (table.CreateIfNotExists())
                                                {
                                                    Console.Write(customername + " Table ");
                                                    Console.WriteLine("Created ");
                                                }
                                                else
                                                {
                                                    Console.WriteLine(customername + " Table not created");
                                                }
                                            }
                                            catch (StorageException s)
                                            {
                                                Console.WriteLine("Refresh and retry " + s.ToString());
                                                return;
                                            }   
                                        }
                                        count++;

                                    }
                                    else if (attribute == "value")
                                        Value = reader.Value.Trim();
                                    else if (attribute == "id")
                                    {
                                        Id = reader.Value.Trim();
                                        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                                        string t1 = DateTime.Now.ToString();
                                        details d1 = new details(customername, t.TotalMilliseconds.ToString());
                                        d1.id = Id;
                                        d1.devicefunction = DeviceFunction;
                                        d1.devicename = DeviceName;
                                        d1.devicetype = DeviceType;
                                        d1.devtabid = DeviceTableID;
                                        d1.ep = Endpoint;
                                        d1.vendorname = Vendorname;
                                        d1.IEEE = IEEE;
                                        d1.datetime = t1;
                                        d1.energy1 = Energy_On;
                                        d1.energy2 = Energy_off;
                                        
                                        TableOperation insertOperation = TableOperation.Insert(d1);
                                        // Execute the insert operation.
                                        try
                                        {
                                           
                                            table.Execute(insertOperation);
                                            Console.WriteLine(customername + "     Record Inserted");
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e.ToString());
                                            continue;
                                        }

                                    }
                                }
                            break;
                        }
                    }
                }
            }
        }
        public void retrive_from_builder_customer()
        {
            
            string[] arr = new string[100];
            arr = rd1.Split('-');


            string tableName = "builder";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            CloudTableClient tableClient1 = storageAccount.CreateCloudTableClient();
            CloudTable table1 = null;
            try
            {
                table1 = tableClient1.GetTableReference(tableName);
                TableQuery<BuilderEntity> Query = new TableQuery<BuilderEntity>().Where(
                TableQuery.CombineFilters(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, arr[3]),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("buildername", QueryComparisons.Equal, arr[2])), TableOperators.And, TableQuery.GenerateFilterCondition("location", QueryComparisons.Equal, arr[4])));
                List<BuilderEntity> AddressList = new List<BuilderEntity>();
                BuilderEntity bd = new BuilderEntity();
                File.Delete("D:\\retrive.txt");
                using (StreamWriter writer =
                new StreamWriter("D:\\retrive.txt",true))
                {
                    foreach (BuilderEntity entity in table1.ExecuteQuery(Query))
                    {
                        bd.customername = entity.customername;
                        writer.Write(bd.customername);
                        writer.Write("-");
                    }
                    writer.Close();
                }
              
            }
           catch (Exception e)
            {
                Console.WriteLine("TABLE NAME= " + tableName + "  doesnot exist ");
                Console.WriteLine("Send again.............");
                processfun();
         
            }
            string data;
            System.IO.StreamReader file1 = new System.IO.StreamReader("D:\\retrive.txt");
            data = file1.ReadToEnd();
            Console.WriteLine(data);
            file1.Close();
            TcpClient newclient = new TcpClient();
            Console.WriteLine("Trying to connect........!");
            try
            {
                newclient.Connect(arr[1], 8902);

                Console.WriteLine("Connected to " + arr[1]);
                StreamWriter sw = new StreamWriter(newclient.GetStream());
                sw.WriteLine(data.ToString());
                sw.Flush();
                newclient.Close();
                Console.WriteLine("Successfully transfered.....!");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                processfun();
            }
        }

        public void receive_from_table()
        {
        
            lock (this)
            {
              
                string ipaddress = null;
                string[] arr = new string[100];
                arr = rd1.Split('-');
                Console.WriteLine("IP Address : " + arr[0]);
                Console.WriteLine("Custmer Name : " + arr[1]);
                Console.WriteLine("Time of day : " + arr[2]);
                ipaddress = arr[0];
                string tableName = arr[1] + arr[2];
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
                CloudTableClient tableClient1 = storageAccount.CreateCloudTableClient();
                CloudTable table1 = null;
                try
                {
                    table1 = tableClient1.GetTableReference(tableName);
                    TableQuery<CustomerEntity> query = new TableQuery<CustomerEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "xyz"));
                    List<CustomerEntity> AddressList = new List<CustomerEntity>();
                    
                    foreach (CustomerEntity entity in table1.ExecuteQuery(query))
                    {
                        CustomerEntity ce = new CustomerEntity();
                        ce.PartitionKey = entity.PartitionKey;
                        ce.RowKey = entity.RowKey;
                        ce.id = entity.id;
                        ce.devicefunction = entity.devicefunction;
                        ce.devicename = entity.devicename;
                        ce.devicetype = entity.devicetype;
                        ce.devtabid = entity.devtabid;
                        ce.ep = entity.ep;
                        ce.vendorname = entity.vendorname;
                        ce.IEEE = entity.IEEE;
                        ce.datetime = entity.datetime;
                        ce.energy1 = entity.energy1;
                        ce.energy2 = entity.energy2;
                        AddressList.Add(ce);
                    }
                    XmlSerializer serializer = new XmlSerializer(typeof(List<CustomerEntity>));
                    using (TextWriter writer = new StreamWriter("D:\\customerinfofromtable.xml"))
                    {
                        serializer.Serialize(writer, AddressList);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("TABLE NAME= " + tableName + "  doesnot exist ");
                    Console.WriteLine("Send again.............");
                    processfun();
                }
                string data;
                System.IO.StreamReader file1 = new System.IO.StreamReader("D:\\customerinfofromtable.xml");
                data = file1.ReadToEnd();
                //Console.WriteLine(data);


                file1.Close();
                TcpClient newclient = new TcpClient();
                Console.WriteLine("Trying to connect........!");
                newclient.Connect(arr[0], 8902);
                Console.WriteLine("Connected to " + arr[1]);
                StreamWriter sw = new StreamWriter(newclient.GetStream());
                sw.WriteLine(data.ToString());
                sw.Flush();
                newclient.Close();
                Console.WriteLine("Successfully transfered.....!");
            }
        }

        public void send_to_builder()
        {
            string[] arr = new string[100];
            arr = rd1.Split('-');
       
            var date = DateTime.Now;
            table = tableClient.GetTableReference("builder");
            try
            {
                 if (table.CreateIfNotExists())
                 {
                        Console.Write("builder" + " Table ");
                        Console.WriteLine("Created ");
                 }
                 else
                 {
                        Console.WriteLine("builder" + " Table not created");
                 }
            }
            catch (StorageException s)
            {
                  Console.WriteLine("Refresh and retry " + s.ToString());
                  return;
            }
                                  

            BuilderEntity bd = new BuilderEntity(arr[2],arr[6]);
            bd.buildername = arr[1];
            bd.contactnumber = arr[5];
            bd.customername = arr[3];
            bd.location = arr[4];
            TableOperation insertOperation = TableOperation.Insert(bd);
            // Execute the insert operation.
            try
            {
                                       
                  table.Execute(insertOperation);
                                        
                  Console.WriteLine(arr[2]+" "+ arr[6] + "     Record Inserted");
                                     
            }
            catch (Exception e)
            {
                  Console.WriteLine(e.ToString());
                                  
            }
        }
        public void send_customer_info_to_builder()
        {
            lock (this)
            {

                string ipaddress = null;
                string[] arr = new string[100];
                arr = rd1.Split('-');
                Console.WriteLine("IP Address : " + arr[1]);
                Console.WriteLine("Custmer Name : " + arr[2]);
                Console.WriteLine("Time of day : " + arr[3]);
                ipaddress = arr[1];
                string tableName = arr[2] + arr[3];
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
                CloudTableClient tableClient1 = storageAccount.CreateCloudTableClient();
                CloudTable table1 = null;
                try
                {
                    table1 = tableClient1.GetTableReference(tableName);
                    TableQuery<CustomerEntity> query = new TableQuery<CustomerEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "xyz"));
                    List<CustomerEntity> AddressList1 = new List<CustomerEntity>();
                    //CustomerEntity ce = new CustomerEntity();
                    //AddressList = null;
                    File.Delete("D:\\customerinfo.xml");
                    foreach (CustomerEntity entity in table1.ExecuteQuery(query))
                    {
                        CustomerEntity ce=new CustomerEntity();
                        ce.PartitionKey = entity.PartitionKey;
                        ce.RowKey = entity.RowKey;
                        ce.id = entity.id;
                        ce.devicefunction = entity.devicefunction;
                        ce.devicename = entity.devicename;
                        ce.devicetype = entity.devicetype;
                        ce.devtabid = entity.devtabid;
                        ce.ep = entity.ep;
                        ce.vendorname = entity.vendorname;
                        ce.IEEE = entity.IEEE;
                        ce.energy1 = entity.energy1;
                        ce.energy2 = entity.energy2;
                        ce.datetime = entity.datetime;
                       AddressList1.Add(ce);
                       
                        
                       
                    }
                    XmlSerializer serializer = new XmlSerializer(typeof(List<CustomerEntity>));
                    using (TextWriter writer = new StreamWriter("D:\\customerinfo.xml", true))
                    {
                        serializer.Serialize(writer, AddressList1);

                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("TABLE NAME= " + tableName + "  doesnot exist "+ e.ToString());
                    Console.WriteLine("Send again.............");
                    processfun();
                }
                string data;
                System.IO.StreamReader file1 = new System.IO.StreamReader("D:\\customerinfo.xml");
                data = file1.ReadToEnd();
                //Console.WriteLine(data);


                file1.Close();
                TcpClient newclient = new TcpClient();
                Console.WriteLine("Trying to connect........!");
                try
                {
                    newclient.Connect(arr[1], 8902);
                    Console.WriteLine("Connected to " + arr[1]);
                    StreamWriter sw = new StreamWriter(newclient.GetStream());
                    sw.WriteLine(data.ToString());
                    sw.Flush();
                    newclient.Close();
                    Console.WriteLine("Successfully transfered.....!");
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        static void Main(string[] args)
        {
            Program p = new Program();
            details[] customer = new details[100];
            Console.WriteLine("Start");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            tableClient = storageAccount.CreateCloudTableClient();
            //Console.WriteLine("Enter port number to make connnection......!");
           // Int32 port = Convert.ToInt32(Console.ReadLine());
            while (true)
            {
                p.processfun();
            }
        }
    }
}
