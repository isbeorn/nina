using Microsoft.VisualBasic.FileIO;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDataImport {
    class Program {

        public class DatabaseInteraction {
            string _connectionString = @"Data Source=" + AppDomain.CurrentDomain.BaseDirectory + @"\Database\NINA.sqlite;foreign keys=true;";

            public DatabaseInteraction() {
                _connection = new SQLiteConnection(_connectionString);
            }

            SQLiteConnection _connection;

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

                if(!File.Exists(dbfile)) {
                    SQLiteConnection.CreateFile(dbfile);
                }
                
            }
        }
        


        static void Main(string[] args) {
            var db = new DatabaseInteraction();
            db.CreateDatabase();

            db.GenericQuery("DROP TABLE IF EXISTS visualdescription");
            db.GenericQuery("DROP TABLE IF EXISTS cataloguenr");
            db.GenericQuery("DROP TABLE IF EXISTS dsodetail;");
            

            db.GenericQuery(@"CREATE TABLE IF NOT EXISTS dsodetail (
                id integer NOT NULL,
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
                dsodetailid INT,
                description TEXT,
                PRIMARY KEY (dsodetailid, description),
                FOREIGN KEY (dsodetailid) REFERENCES dsodetail (id)
            );");

            db.GenericQuery(@"CREATE TABLE IF NOT EXISTS cataloguenr (                                
                dsodetailid INT,
                catalogue TEXT,
                designation TEXT,
                PRIMARY KEY (dsodetailid, catalogue, designation),
                FOREIGN KEY (dsodetailid) REFERENCES dsodetail (id)
            );");


            




            using (TextFieldParser parser = new TextFieldParser(@"SAC_DeepSky_ver81_Excel.csv")) {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
               

                HashSet<string> types = new HashSet<string>();
                var isFirst = true;
                List<DSO> l = new List<DSO>();
                var i = 1;
                while (!parser.EndOfData) {
                    string[] fields = parser.ReadFields();
                    //Processing row
                    if (isFirst) {
                        isFirst = false;
                        continue;
                    }
                  
                    DSO dso = new DSO(i++, fields);
                    if(dso.cataloguenr.First().catalogue != null) {
                        l.Add(dso);
                    }
                    Console.WriteLine(i);
                    dso.insert(db);
                    
                }

                var err = l.Where((x) => x.cataloguenr.Count == 0);
            }


        }

       

        class cataloguenr {
            public string catalogue;
            public string designation;

            public cataloguenr(string field) {                
                catalogue = catalogues.Where((x) => field.StartsWith(x)).FirstOrDefault();
                if(catalogue != null) {
                    catalogue = catalogue.Trim();
                    designation = field.Split(new string[] { catalogue }, StringSplitOptions.None)[1].Trim();
                }
            }

            private string[] catalogues = {
                "3C","Abell","ADS","AM","Antalova","Ap","Arp","Bark","B","Basel","BD","Berk","Be","Biur","Blanco","Bochum","Ced","CGCG","Cr","Czernik","DDO","Do","DoDz","Dun","ESO","Fein","Frolov","Gum","H","Haffner","Harvard","Hav-Moffat","He","Hogg","Ho","HP","Hu","IC","Isk","J","K","Kemble","King","Kr","Lac","Loden","LBN","LDN","NPM1G","Lynga","M","MCG","Me","Mrk","Mel","M1 thru M4","New","NGC","Pal","PB","PC","Pismis","PK","RCW","Roslund","Ru","Sa","Sher","Sh","SL","SL","Steph","Stock","Ter","Tombaugh","Ton","Tr","UGC","UGCA","UKS","Upgren","V V","vdB","vdBH","vdB-Ha","Vy","Waterloo","Winnecke","ZwG"
            };

            public override string ToString() {
                return catalogue + " " + designation;
            }
        }



        class DSO {
            public List<cataloguenr> cataloguenr;
            //public string obj;
            //public string other;
            public int Id;
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
            public string NGC_Descr;
            public string Notes;

            public override string ToString() {
                var s = "";
                foreach(var cat in cataloguenr) {
                    s += cat.ToString() + "; ";
                }
                return s;
            }

            public DSO(int id, string[] fields) {
                this.Id = id;
                cataloguenr = new List<Program.cataloguenr>();

                cataloguenr.Add(new cataloguenr(fields[0]));

                foreach (var field in fields[1].Split(';')) {
                    if(field != string.Empty) {
                        var cat = new cataloguenr(field);
                        if(cataloguenr.Any((x) => x.catalogue == cat.catalogue && x.designation == cat.designation)) {
                            continue;
                        }
                        if(cat.catalogue != null) {
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

                
                RA = Utility.AscomUtil.HMSToDegrees(fields[4]); 
                DEC = Utility.AscomUtil.DMSToDegrees(fields[5]);

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
                NGC_Descr = fields[17];
                Notes = fields[18];
            }

            
            internal void insert(DatabaseInteraction db) {
                var q = $@"INSERT INTO dsodetail 
                (id, ra, dec, magnitude, surfacebrightness,sizemin,sizemax,positionangle,nrofstars,brighteststar,constellation,dsotype,dsoclass,notes)  VALUES
                ({Id}, 
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
                    q += $@"INSERT INTO cataloguenr (dsodetailid, catalogue, designation) VALUES ({Id}, ""{cat.catalogue}"", ""{cat.designation}""); ";
                }
                db.GenericQuery(q);
            }
        }
    }
}
