#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace StarDataImport {

    internal class Program {

        public class DatabaseInteraction {
            private string _connectionString = @"Data Source=" + AppDomain.CurrentDomain.BaseDirectory + @"\Database\NINA.sqlite;foreign keys=true;";

            public DatabaseInteraction() {
                _connection = new SQLiteConnection(_connectionString);
            }

            private SQLiteConnection _connection;

            public int GenericQuery(string query) {
                _connection.Open();

                SQLiteCommand command = new SQLiteCommand(query, _connection);
                var rows = command.ExecuteNonQuery();
                _connection.Close();

                return rows;
            }

            public void CreateDatabase() {
                var dir = AppDomain.CurrentDomain.BaseDirectory + @"\Database";
                var dbfile = dir + @"\NINA.sqlite";
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(dbfile)) {
                    SQLiteConnection.CreateFile(dbfile);
                }
            }

            public void BulkInsert(ICollection<string> queries) {
                _connection.Open();
                using (SQLiteCommand cmd = _connection.CreateCommand()) {
                    using (var transaction = _connection.BeginTransaction()) {
                        foreach (var q in queries) {
                            cmd.CommandText = q;
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                _connection.Close();
            }
        }

        private static void Main(string[] args) {
            GenerateStarDatabase();
            //GenerateDatabase();
            //UpdateStarData();
        }

        public static void UpdateStarData() {
            List<SimpleDSO> objects = new List<SimpleDSO>();
            var connectionString = string.Format(@"Data Source={0};foreign keys=true;", @"D:\Projects\NINA.sqlite");
            var query = "select dsodetailid, catalogue, designation  from cataloguenr INNER JOIN dsodetail ON dsodetail.id = cataloguenr.dsodetailid WHERE syncedfrom is null group by dsodetailid order by catalogue;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString)) {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand()) {
                    command.CommandText = query;

                    var reader = command.ExecuteReader();
                    while (reader.Read()) {
                        objects.Add(new SimpleDSO() { id = reader.GetString(0), name = reader.GetString(1) + " " + reader.GetString(2) });
                    }
                }
            }

            var sw = Stopwatch.StartNew();

            Parallel.ForEach(objects, obj => {
                var _url = "http://cdsws.u-strasbg.fr/axis/services/Sesame";
                var _action = "";

                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(obj.name);
                HttpWebRequest webRequest = CreateWebRequest(_url, _action);
                webRequest.Timeout = -1;
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

                // begin async call to web request.
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

                // suspend this thread until call is complete. You might want to do something usefull
                // here like update your UI.
                asyncResult.AsyncWaitHandle.WaitOne();

                // get the response from the completed web request.

                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult)) {
                    var soap = XElement.Load(webResponse.GetResponseStream());

                    var xmlstring = (from c in soap.Descendants("return") select c).FirstOrDefault()?.Value;
                    var xml = XElement.Parse(xmlstring);
                    var resolvername = "Simbad";
                    var resolver = xml.Descendants("Resolver").Where((x) => x.Attribute("name").Value.Contains(resolvername)).FirstOrDefault();

                    var ra = resolver.Descendants("jradeg").FirstOrDefault()?.Value;
                    var dec = resolver.Descendants("jdedeg").FirstOrDefault()?.Value;

                    if (ra == null) {
                        resolvername = "NED";
                        resolver = xml.Descendants("Resolver").Where((x) => x.Attribute("name").Value.Contains(resolvername)).FirstOrDefault();

                        ra = resolver.Descendants("jradeg").FirstOrDefault()?.Value;
                        dec = resolver.Descendants("jdedeg").FirstOrDefault()?.Value;
                    }

                    if (ra == null) {
                        resolvername = "VizieR";
                        resolver = xml.Descendants("Resolver").Where((x) => x.Attribute("name").Value.Contains(resolvername)).FirstOrDefault();

                        ra = resolver.Descendants("jradeg").FirstOrDefault()?.Value;
                        dec = resolver.Descendants("jdedeg").FirstOrDefault()?.Value;
                    }

                    Console.WriteLine(obj.ToString());
                    if (ra == null) {
                        Console.WriteLine("NO ENTRY");
                    } else {
                        Console.WriteLine("Found " + " RA:" + ra + " DEC:" + dec);
                    }

                    if (ra != null && dec != null) {
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString)) {
                            connection.Open();
                            using (SQLiteCommand command = connection.CreateCommand()) {
                                command.CommandText = "UPDATE dsodetail SET ra = $ra, dec = $dec, syncedfrom = '" + resolvername + "' WHERE id = $id;";
                                command.Parameters.AddWithValue("$id", obj.id);
                                command.Parameters.AddWithValue("$ra", ra);
                                command.Parameters.AddWithValue("$dec", dec);

                                var rows = command.ExecuteNonQuery();
                                Console.WriteLine(string.Format("Inserted {0} row(s)", rows));
                            }
                        }
                    }
                }
            });

            /*foreach(var obj in objects) {
                var _url = "http://cdsws.u-strasbg.fr/axis/services/Sesame";
                var _action = "";

                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(obj.name);
                HttpWebRequest webRequest = CreateWebRequest(_url,_action);
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml,webRequest);

                // begin async call to web request.
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null,null);

                // suspend this thread until call is complete. You might want to do something usefull
                // here like update your UI.
                asyncResult.AsyncWaitHandle.WaitOne();

                // get the response from the completed web request.

                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult)) {
                    var soap = XElement.Load(webResponse.GetResponseStream());

                    var xmlstring = (from c in soap.Descendants("return") select c).FirstOrDefault()?.Value;
                    var xml = XElement.Parse(xmlstring);
                    var simbad = xml.Descendants("Resolver").Where((x) => x.Attribute("name").Value.Contains("Simbad")).FirstOrDefault();

                    var ra = simbad.Descendants("jradeg").FirstOrDefault()?.Value;
                    var dec = simbad.Descendants("jdedeg").FirstOrDefault()?.Value;

                    Console.WriteLine(obj.ToString());
                    if (ra == null) {
                        Console.WriteLine("NO ENTRY");
                    }
                    else {
                        Console.WriteLine("Found " + " RA:" + ra + " DEC:" + dec);
                    }

                    if(ra != null && dec != null) {
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString)) {
                            connection.Open();
                            using (SQLiteCommand command = connection.CreateCommand()) {
                                command.CommandText = "UPDATE dsodetail SET ra = $ra, dec = $dec WHERE id = $id;";
                                command.Parameters.AddWithValue("$id",obj.id);
                                command.Parameters.AddWithValue("$ra",ra);
                                command.Parameters.AddWithValue("$dec",dec);

                                var rows = command.ExecuteNonQuery();
                                Console.WriteLine(string.Format("Inserted {0} row(s)",rows));
                            }
                        }
                    }
                }
            }*/

            Console.WriteLine(sw.Elapsed);

            Console.ReadLine();
        }

        private static HttpWebRequest CreateWebRequest(string url, string action) {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope(string target) {
            XmlDocument soapEnvelopeDocument = new XmlDocument();

            soapEnvelopeDocument.LoadXml(string.Format(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no"" ?>
                                            <SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:apachesoap=""http://xml.apache.org/xml-soap"" xmlns:impl=""http://cdsws.u-strasbg.fr/axis/services/Sesame"" xmlns:intf=""http://cdsws.u-strasbg.fr/axis/services/Sesame"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"" xmlns:wsdl=""http://schemas.xmlsoap.org/wsdl/"" xmlns:wsdlsoap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	                                            <SOAP-ENV:Body>
		                                            <mns:SesameXML xmlns:mns=""http://cdsws.u-strasbg.fr/axis/services/Sesame"" SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
			                                            <name xsi:type=""xsd:string"">{0}</name>
		                                            </mns:SesameXML>
	                                            </SOAP-ENV:Body>
                                            </SOAP-ENV:Envelope>
            ", target));
            return soapEnvelopeDocument;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest) {
            using (Stream stream = webRequest.GetRequestStream()) {
                soapEnvelopeXml.Save(stream);
            }
        }

        private static void GenerateStarDatabase() {
            var db = new DatabaseInteraction();
            db.CreateDatabase();

            db.GenericQuery("DROP TABLE IF EXISTS brightstars");

            db.GenericQuery(@"CREATE TABLE IF NOT EXISTS brightstars (
                name TEXT NOT NULL,
                ra REAL,
                dec REAL,
                magnitude REAL,
                PRIMARY KEY (name)
            );");

            List<string> queries = new List<string>();

            using (TextFieldParser parser = new TextFieldParser(@"brightstars.csv")) {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                HashSet<string> types = new HashSet<string>();
                var isFirst = true;
                List<DatabaseStar> l = new List<DatabaseStar>();
                var i = 1;
                while (!parser.EndOfData) {
                    string[] fields = parser.ReadFields();
                    //Processing row
                    if (isFirst) {
                        isFirst = false;
                        continue;
                    }

                    DatabaseStar dso = new DatabaseStar(fields);

                    queries.Add(dso.getStarQuery());
                }

                db.BulkInsert(queries);

                Console.WriteLine("Done");
                Console.ReadLine();
            }
        }

        public static void GenerateDatabase() {
            var db = new DatabaseInteraction();
            db.CreateDatabase();

            db.GenericQuery("DROP TABLE IF EXISTS visualdescription");
            db.GenericQuery("DROP TABLE IF EXISTS cataloguenr");
            db.GenericQuery("DROP TABLE IF EXISTS dsodetail;");

            db.GenericQuery(@"CREATE TABLE IF NOT EXISTS dsodetail (
                id TEXT NOT NULL,
                ra REAL,
                dec REAL,
                magnitude REAL,
                surfacebrightness REAL,
                sizemin REAL,
                sizemax REAL,
                positionangle REAL,
                nrofstars REAL,
                brighteststar REAL,
                constellation TEXT,
                dsotype TEXT,
                dsoclass TEXT,
                notes TEXT,
                PRIMARY KEY (id)
            );");

            db.GenericQuery(@"CREATE TABLE IF NOT EXISTS visualdescription (
                dsodetailid TEXT,
                description TEXT,
                PRIMARY KEY (dsodetailid, description),
                FOREIGN KEY (dsodetailid) REFERENCES dsodetail (id)
            );");

            db.GenericQuery(@"CREATE TABLE IF NOT EXISTS cataloguenr (
                dsodetailid TEXT,
                catalogue TEXT,
                designation TEXT,
                PRIMARY KEY (dsodetailid, catalogue, designation),
                FOREIGN KEY (dsodetailid) REFERENCES dsodetail (id)
            );");

            List<string> queries = new List<string>();

            using (TextFieldParser parser = new TextFieldParser(@"SAC_DeepSky_ver81_Excel.csv")) {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                HashSet<string> types = new HashSet<string>();
                var isFirst = true;
                List<DatabaseDSO> l = new List<DatabaseDSO>();
                var i = 1;
                while (!parser.EndOfData) {
                    string[] fields = parser.ReadFields();
                    //Processing row
                    if (isFirst) {
                        isFirst = false;
                        continue;
                    }

                    DatabaseDSO dso = new DatabaseDSO(i++, fields);
                    if (dso.cataloguenr.First().catalogue != null) {
                        l.Add(dso);
                    }

                    queries.Add(dso.getDSOQuery());
                    queries.Add(dso.getCatalogueQuery());
                    queries.Add(dso.getVisualDescriptionQuery());
                }

                var duplicates = l.Where(s => s.Id == string.Empty);
            }

            db.BulkInsert(queries);

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private class cataloguenr {
            public string catalogue;
            public string designation;

            public cataloguenr(string field) {
                catalogue = catalogues.Where((x) => field.StartsWith(x)).FirstOrDefault();
                if (catalogue != null) {
                    catalogue = catalogue.Trim();
                    designation = field.Split(new string[] { catalogue }, StringSplitOptions.None)[1].Trim();
                }
            }

            private string[] catalogues = {
                "3C","Archinal", "Abell","ADS","AM","Antalova", "Auner", "Av-Hunter", "AND","Ap","Arp","Bark","Basel","BD","Berk","Be","Biur","Blanco","Bochum","B","Ced","CGCG","Cr", "Coalsack","Czernik","Danks", "DDO","DoDz","Do","Dun","ESO", "Eridanus Cluster","Fein","Frolov","Graham", "Gum","Haffner","Harvard","Hav-Moffat","He","Hogg","Ho","HP","Hu","H","IC","Isk","J","Kemble","King","Kr","K","Latysev","Lg Magellanic Cl", "Le Gentil", "Lac","Loden","LBN","LDN","NPM1G","Lynga","MCG","Me","Mrk","Mel","M1 thru M4","M","New","NGC","Pal","PB","PC","Pismis","PK","RCW","Roslund","Ru","Sa","Sher","Sh","SL","Steph","Stock","Ter","Tombaugh","Ton","Tr","UGC","UGCA","UKS","Upgren","V V","vdB-Ha","vdBH","vdB","Vy", "VY","Waterloo","Winnecke","ZwG"
            };

            public override string ToString() {
                return catalogue + designation;
            }
        }

        public class visualdescription {
            public string description;

            public visualdescription(string field) {
                description = field;
            }
        }

        private class SimpleDSO {
            public string id;
            public string name;

            public override string ToString() {
                return name;
            }
        }

        private class DatabaseStar {

            private static readonly Lazy<ASCOM.Utilities.Util> lazyAscomUtil =
            new Lazy<ASCOM.Utilities.Util>(() => new ASCOM.Utilities.Util());

            private static ASCOM.Utilities.Util AscomUtil { get { return lazyAscomUtil.Value; } }

            //public string obj;
            //public string other;
            public string Name;

            public double RA;
            public double DEC;
            public double magnitude;

            public DatabaseStar(string[] fields) {
                this.Name = fields[0];

                RA = AscomUtil.HMSToDegrees(fields[2]);
                DEC = double.Parse(fields[3], CultureInfo.InvariantCulture);

                magnitude = double.Parse(fields[1], CultureInfo.InvariantCulture);
            }

            public string getStarQuery() {
                return $@"INSERT INTO brightstars
                (name, ra, dec, magnitude)  VALUES
                (""{Name}"",
                {RA.ToString(CultureInfo.InvariantCulture)},
                {DEC.ToString(CultureInfo.InvariantCulture)},
                {magnitude.ToString(CultureInfo.InvariantCulture)}); ";
            }

            /*internal void insert(DatabaseInteraction db) {
                var q = $@"INSERT INTO dsodetail
                (id, ra, dec, magnitude, surfacebrightness,sizemin,sizemax,positionangle,nrofstars,brighteststar,constellation,dsotype,dsoclass,notes)  VALUES
                ({Name},
                {RA.ToString(CultureInfo.InvariantCulture)},
                {DEC.ToString(CultureInfo.InvariantCulture)},
                {magnitude.ToString(CultureInfo.InvariantCulture)},
                {subr.ToString(CultureInfo.InvariantCulture)},
                {size_min?.ToString(CultureInfo.InvariantCulture) ?? "null"},
                {size_max?.ToString(CultureInfo.InvariantCulture) ?? "null"},
                ""{positionangle}"",
                ""{NSTS}"",
                ""{brighteststar}"",
                ""{constellation}"",
                ""{type}"",
                ""{classification}"",
                ""{Notes}"" ); ";
                db.GenericQuery(q);

                q = "";
                foreach (var cat in cataloguenr) {
                    q += $@"INSERT INTO cataloguenr (dsodetailid, catalogue, designation) VALUES ({Name}, ""{cat.catalogue}"", ""{cat.designation}""); ";
                }
                db.GenericQuery(q);

                q = "";
                foreach (var desc in visualdescription) {
                    q += $@"INSERT INTO visualdescription (dsodetailid, description) VALUES ({Name}, ""{desc.description}""); ";
                }
                db.GenericQuery(q);
            }*/
        }

        private class DatabaseDSO {

            private static readonly Lazy<ASCOM.Utilities.Util> lazyAscomUtil =
            new Lazy<ASCOM.Utilities.Util>(() => new ASCOM.Utilities.Util());

            private static ASCOM.Utilities.Util AscomUtil { get { return lazyAscomUtil.Value; } }

            public List<cataloguenr> cataloguenr;

            //public string obj;
            //public string other;
            public string Id;

            public string type;
            public string constellation;
            public double RA;
            public double DEC;
            public double magnitude;
            public double subr;
            public string u2k;
            public string ti;
            public double? size_max;
            public double? size_min;
            public string positionangle;
            public string classification;
            public string NSTS;
            public string brighteststar;
            public string CHM;
            public List<visualdescription> visualdescription;
            public string Notes;

            public override string ToString() {
                var s = "";
                foreach (var cat in cataloguenr) {
                    s += cat.ToString() + "; ";
                }
                return s;
            }

            public DatabaseDSO(int id, string[] fields) {
                cataloguenr = new List<Program.cataloguenr>();
                var ident = new cataloguenr(fields[0]);
                this.Id = ident.ToString();
                if (this.Id == string.Empty) {
                    Debugger.Break();
                }
                cataloguenr.Add(ident);

                foreach (var field in fields[1].Split(';')) {
                    if (field != string.Empty) {
                        var cat = new cataloguenr(field);
                        if (cataloguenr.Any((x) => x.catalogue == cat.catalogue && x.designation == cat.designation)) {
                            continue;
                        }
                        if (cat.catalogue != null) {
                            cataloguenr.Add(cat);
                        } else {
                        }
                    }
                }
                /*cataloguenr.Add(new Program.cataloguenr() { catalogue = fields[0].Split(' ')[0], designation = fields[0].Split(' ')[1] });

                foreach(var field in fields[1].Split(';')) {
                    if(field != string.Empty) {
                        if(field.Split(' ').Length == 1) { continue; }
                        cataloguenr.Add(new Program.cataloguenr() { catalogue = field.Split(' ')[0], designation = field.Split(' ')[1] });
                    }
                }*/

                //other = fields[1];

                type = fields[2];
                constellation = fields[3];

                RA = AscomUtil.HMSToDegrees(fields[4]);
                DEC = AscomUtil.DMSToDegrees(fields[5]);

                magnitude = double.Parse(fields[6], CultureInfo.CreateSpecificCulture("de-DE"));
                subr = double.Parse(fields[7], CultureInfo.CreateSpecificCulture("de-DE"));
                u2k = fields[8];
                ti = fields[9];

                var size = fields[10];
                if (size.Contains("m")) {
                    size_max = double.Parse(size.Replace("m", string.Empty).Trim(), CultureInfo.InvariantCulture) * 60;
                } else if (size.Contains("s")) {
                    size_max = double.Parse(size.Replace("s", string.Empty).Trim(), CultureInfo.InvariantCulture);
                } else {
                    size_max = null;
                }

                size = fields[11];
                if (size.Contains("m")) {
                    size_min = double.Parse(size.Replace("m", string.Empty).Trim(), CultureInfo.InvariantCulture) * 60;
                } else if (size.Contains("s")) {
                    size_min = double.Parse(size.Replace("s", string.Empty).Trim(), CultureInfo.InvariantCulture);
                } else {
                    size_min = null;
                }

                positionangle = fields[12];
                classification = fields[13];
                NSTS = fields[14];
                brighteststar = fields[15];
                CHM = fields[16];

                visualdescription = new List<visualdescription>();
                if (fields[17] != string.Empty) {
                    foreach (var s in fields[17].Split(';')) {
                        visualdescription.Add(new visualdescription(s));
                    }
                }

                Notes = fields[18];
            }

            public string getDSOQuery() {
                return $@"INSERT INTO dsodetail
                (id, ra, dec, magnitude, surfacebrightness,sizemin,sizemax,positionangle,nrofstars,brighteststar,constellation,dsotype,dsoclass,notes)  VALUES
                (""{Id}"",
                {RA.ToString(CultureInfo.InvariantCulture)},
                {DEC.ToString(CultureInfo.InvariantCulture)},
                {magnitude.ToString(CultureInfo.InvariantCulture)},
                {subr.ToString(CultureInfo.InvariantCulture)},
                {size_min?.ToString(CultureInfo.InvariantCulture) ?? "null"},
                {size_max?.ToString(CultureInfo.InvariantCulture) ?? "null"},
                ""{positionangle}"",
                ""{NSTS}"",
                ""{brighteststar}"",
                ""{constellation}"",
                ""{type}"",
                ""{classification}"",
                ""{Notes}"" ); ";
            }

            public string getCatalogueQuery() {
                var q = "";
                foreach (var cat in cataloguenr) {
                    if (cat.catalogue != null && cat.catalogue.Trim() != string.Empty) {
                        q += $@"INSERT INTO cataloguenr (dsodetailid, catalogue, designation) VALUES (""{Id}"", ""{cat.catalogue}"", ""{cat.designation}""); ";
                    }
                }
                return q;
            }

            public string getVisualDescriptionQuery() {
                var q = "";
                foreach (var desc in visualdescription) {
                    if (desc.description != null && desc.description.Trim() != string.Empty) {
                        q += $@"INSERT INTO visualdescription (dsodetailid, description) VALUES (""{Id}"", ""{desc.description}""); ";
                    }
                }
                return q;
            }

            /*internal void insert(DatabaseInteraction db) {
                var q = $@"INSERT INTO dsodetail
                (id, ra, dec, magnitude, surfacebrightness,sizemin,sizemax,positionangle,nrofstars,brighteststar,constellation,dsotype,dsoclass,notes)  VALUES
                ({Name},
                {RA.ToString(CultureInfo.InvariantCulture)},
                {DEC.ToString(CultureInfo.InvariantCulture)},
                {magnitude.ToString(CultureInfo.InvariantCulture)},
                {subr.ToString(CultureInfo.InvariantCulture)},
                {size_min?.ToString(CultureInfo.InvariantCulture) ?? "null"},
                {size_max?.ToString(CultureInfo.InvariantCulture) ?? "null"},
                ""{positionangle}"",
                ""{NSTS}"",
                ""{brighteststar}"",
                ""{constellation}"",
                ""{type}"",
                ""{classification}"",
                ""{Notes}"" ); ";
                db.GenericQuery(q);

                q = "";
                foreach (var cat in cataloguenr) {
                    q += $@"INSERT INTO cataloguenr (dsodetailid, catalogue, designation) VALUES ({Name}, ""{cat.catalogue}"", ""{cat.designation}""); ";
                }
                db.GenericQuery(q);

                q = "";
                foreach (var desc in visualdescription) {
                    q += $@"INSERT INTO visualdescription (dsodetailid, description) VALUES ({Name}, ""{desc.description}""); ";
                }
                db.GenericQuery(q);
            }*/
        }
    }
}