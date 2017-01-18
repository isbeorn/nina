using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDataImport {
    class Program {



        static void Main(string[] args) {
            using (TextFieldParser parser = new TextFieldParser(@"SAC_DeepSky_ver81_Excel.csv")) {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                DataSet1 db = new DataSet1();
                
                createdsoTypes(db);
                createcatalogues(db);

                HashSet<string> types = new HashSet<string>();

                List<DSO> l = new List<DSO>();
                while (!parser.EndOfData) {
                    //Processing row
                    string[] fields = parser.ReadFields();
                    DSO dso = new DSO(fields);
                    l.Add(dso);

                    if(!types.Contains(dso.type)) {
                        types.Add(dso.type);

                    }




                }
            }
            

        }

        static void adddsotyperow(DataSet1.dsotypeDataTable dsotypes, string key, string name) {
            var row = dsotypes.NewdsotypeRow();
            row.key = key;
            row.name = name;
            dsotypes.AdddsotypeRow(row);
        }

        static void createcatalogues(DataSet1 db) {
            DataSet1TableAdapters.constellationTableAdapter adap = new DataSet1TableAdapters.constellationTableAdapter();
            adap.Fill(db.constellation);

            DataSet1.constellationDataTable constellations = db.constellation;

            var result = from myRow in constellations.AsEnumerable()
                          select myRow.key;


            if (!result.Contains("AND")) {
                addconstellationrow(constellations, "AND", "ANDROMEDA");
            }
            if (!result.Contains("ANT")) {
                addconstellationrow(constellations, "ANT", "ANTLIA");
            }
            if (!result.Contains("APS")) {
                addconstellationrow(constellations, "APS", "APUS");
            }
            if (!result.Contains("AQR")) {
                addconstellationrow(constellations, "AQR", "AQUARIUS");
            }
            if (!result.Contains("AQL")) {
                addconstellationrow(constellations, "AQL", "AQUILA");
            }
            if (!result.Contains("ARA")) {
                addconstellationrow(constellations, "ARA", "ARA");
            }
            if (!result.Contains("ARI")) {
                addconstellationrow(constellations, "ARI", "ARIES");
            }
            if (!result.Contains("AUR")) {
                addconstellationrow(constellations, "AUR", "AURIGA");
            }
            if (!result.Contains("BOO")) {
                addconstellationrow(constellations, "BOO", "BOOTES");
            }
            if (!result.Contains("CAE")) {
                addconstellationrow(constellations, "CAE", "CAELUM");
            }
            if (!result.Contains("CAM")) {
                addconstellationrow(constellations, "CAM", "CAMELOPARDALIS");
            }
            if (!result.Contains("CNC")) {
                addconstellationrow(constellations, "CNC", "CANCER");
            }
            if (!result.Contains("VENATICI")) {
                addconstellationrow(constellations, "VENATICI", "CANES");
            }
            if (!result.Contains("MAJOR")) {
                addconstellationrow(constellations, "MAJOR", "CANIS");
            }
            if (!result.Contains("MINOR")) {
                addconstellationrow(constellations, "MINOR", "CANIS");
            }
            if (!result.Contains("CAP")) {
                addconstellationrow(constellations, "CAP", "CAPRICORNUS");
            }
            if (!result.Contains("CAR")) {
                addconstellationrow(constellations, "CAR", "CARINA");
            }
            if (!result.Contains("CAS")) {
                addconstellationrow(constellations, "CAS", "CASSIOPEIA");
            }
            if (!result.Contains("CEN")) {
                addconstellationrow(constellations, "CEN", "CENTAURUS");
            }
            if (!result.Contains("CEP")) {
                addconstellationrow(constellations, "CEP", "CEPHEUS");
            }
            if (!result.Contains("CET")) {
                addconstellationrow(constellations, "CET", "CETUS");
            }
            if (!result.Contains("CHA")) {
                addconstellationrow(constellations, "CHA", "CHAMAELEON");
            }
            if (!result.Contains("CIR")) {
                addconstellationrow(constellations, "CIR", "CIRCINUS");
            }
            if (!result.Contains("COL")) {
                addconstellationrow(constellations, "COL", "COLUMBA");
            }
            if (!result.Contains("BERENICES")) {
                addconstellationrow(constellations, "BERENICES", "COMA");
            }
            if (!result.Contains("AUSTRALIS")) {
                addconstellationrow(constellations, "AUSTRALIS", "CORONA");
            }
            if (!result.Contains("BOREALIS")) {
                addconstellationrow(constellations, "BOREALIS", "CORONA");
            }
            if (!result.Contains("CRV")) {
                addconstellationrow(constellations, "CRV", "CORVUS");
            }
            if (!result.Contains("CRT")) {
                addconstellationrow(constellations, "CRT", "CRATER");
            }
            if (!result.Contains("CRU")) {
                addconstellationrow(constellations, "CRU", "CRUX");
            }
            if (!result.Contains("CYG")) {
                addconstellationrow(constellations, "CYG", "CYGNUS");
            }
            if (!result.Contains("DEL")) {
                addconstellationrow(constellations, "DEL", "DELPHINUS");
            }
            if (!result.Contains("DOR")) {
                addconstellationrow(constellations, "DOR", "DORADO");
            }
            if (!result.Contains("DRA")) {
                addconstellationrow(constellations, "DRA", "DRACO");
            }
            if (!result.Contains("EQU")) {
                addconstellationrow(constellations, "EQU", "EQUULEUS");
            }
            if (!result.Contains("ERI")) {
                addconstellationrow(constellations, "ERI", "ERIDANUS");
            }
            if (!result.Contains("FOR")) {
                addconstellationrow(constellations, "FOR", "FORNAX");
            }
            if (!result.Contains("GEM")) {
                addconstellationrow(constellations, "GEM", "GEMINI");
            }
            if (!result.Contains("GRU")) {
                addconstellationrow(constellations, "GRU", "GRUS");
            }
            if (!result.Contains("HER")) {
                addconstellationrow(constellations, "HER", "HERCULES");
            }
            if (!result.Contains("HOR")) {
                addconstellationrow(constellations, "HOR", "HOROLOGIUM");
            }
            if (!result.Contains("HYA")) {
                addconstellationrow(constellations, "HYA", "HYDRA");
            }
            if (!result.Contains("HYI")) {
                addconstellationrow(constellations, "HYI", "HYDRUS");
            }
            if (!result.Contains("IND")) {
                addconstellationrow(constellations, "IND", "INDUS");
            }
            if (!result.Contains("LAC")) {
                addconstellationrow(constellations, "LAC", "LACERTA");
            }
            if (!result.Contains("LEO")) {
                addconstellationrow(constellations, "LEO", "LEO");
            }
            if (!result.Contains("MINOR")) {
                addconstellationrow(constellations, "MINOR", "LEO");
            }
            if (!result.Contains("LEP")) {
                addconstellationrow(constellations, "LEP", "LEPUS");
            }
            if (!result.Contains("LIB")) {
                addconstellationrow(constellations, "LIB", "LIBRA");
            }
            if (!result.Contains("LUP")) {
                addconstellationrow(constellations, "LUP", "LUPUS");
            }
            if (!result.Contains("LYN")) {
                addconstellationrow(constellations, "LYN", "LYNX");
            }
            if (!result.Contains("LYR")) {
                addconstellationrow(constellations, "LYR", "LYRA");
            }
            if (!result.Contains("MEN")) {
                addconstellationrow(constellations, "MEN", "MENSA");
            }
            if (!result.Contains("MIC")) {
                addconstellationrow(constellations, "MIC", "MICROSCOPIUM");
            }
            if (!result.Contains("MON")) {
                addconstellationrow(constellations, "MON", "MONOCEROS");
            }
            if (!result.Contains("MUS")) {
                addconstellationrow(constellations, "MUS", "MUSCA");
            }
            if (!result.Contains("NOR")) {
                addconstellationrow(constellations, "NOR", "NORMA");
            }
            if (!result.Contains("OCT")) {
                addconstellationrow(constellations, "OCT", "OCTANS");
            }
            if (!result.Contains("OPH")) {
                addconstellationrow(constellations, "OPH", "OPHIUCHUS");
            }
            if (!result.Contains("ORI")) {
                addconstellationrow(constellations, "ORI", "ORION");
            }
            if (!result.Contains("PAV")) {
                addconstellationrow(constellations, "PAV", "PAVO");
            }
            if (!result.Contains("PEG")) {
                addconstellationrow(constellations, "PEG", "PEGASUS");
            }
            if (!result.Contains("PER")) {
                addconstellationrow(constellations, "PER", "PERSEUS");
            }
            if (!result.Contains("PHE")) {
                addconstellationrow(constellations, "PHE", "PHOENIX");
            }
            if (!result.Contains("PIC")) {
                addconstellationrow(constellations, "PIC", "PICTOR");
            }
            if (!result.Contains("PSC")) {
                addconstellationrow(constellations, "PSC", "PISCES");
            }
            if (!result.Contains("AUSTRINUS")) {
                addconstellationrow(constellations, "AUSTRINUS", "PISCES");
            }
            if (!result.Contains("PUP")) {
                addconstellationrow(constellations, "PUP", "PUPPIS");
            }
            if (!result.Contains("PYX")) {
                addconstellationrow(constellations, "PYX", "PYXIS");
            }
            if (!result.Contains("RET")) {
                addconstellationrow(constellations, "RET", "RETICULUM");
            }
            if (!result.Contains("SGE")) {
                addconstellationrow(constellations, "SGE", "SAGITTA");
            }
            if (!result.Contains("SGR")) {
                addconstellationrow(constellations, "SGR", "SAGITTARIUS");
            }
            if (!result.Contains("SCO")) {
                addconstellationrow(constellations, "SCO", "SCORPIUS");
            }
            if (!result.Contains("SCL")) {
                addconstellationrow(constellations, "SCL", "SCULPTOR");
            }
            if (!result.Contains("SCT")) {
                addconstellationrow(constellations, "SCT", "SCUTUM");
            }
            if (!result.Contains("SER")) {
                addconstellationrow(constellations, "SER", "SERPENS");
            }
            if (!result.Contains("SEX")) {
                addconstellationrow(constellations, "SEX", "SEXTANS");
            }
            if (!result.Contains("TAU")) {
                addconstellationrow(constellations, "TAU", "TAURUS");
            }
            if (!result.Contains("TEL")) {
                addconstellationrow(constellations, "TEL", "TELESCOPIUM");
            }
            if (!result.Contains("TRI")) {
                addconstellationrow(constellations, "TRI", "TRIANGULUM");
            }
            if (!result.Contains("AUSTRALE")) {
                addconstellationrow(constellations, "AUSTRALE", "TRIANGULUM");
            }
            if (!result.Contains("TUC")) {
                addconstellationrow(constellations, "TUC", "TUCANA");
            }
            if (!result.Contains("MAJOR")) {
                addconstellationrow(constellations, "MAJOR", "URSA");
            }
            if (!result.Contains("MINOR")) {
                addconstellationrow(constellations, "MINOR", "URSA");
            }
            if (!result.Contains("VEL")) {
                addconstellationrow(constellations, "VEL", "VELA");
            }
            if (!result.Contains("VIR")) {
                addconstellationrow(constellations, "VIR", "VIRGO");
            }
            if (!result.Contains("VOL")) {
                addconstellationrow(constellations, "VOL", "VOLANS");
            }
            if (!result.Contains("VUL")) {
                addconstellationrow(constellations, "VUL", "VULPECULA");
            }

            adap.Update(constellations);
        }

        private static void addconstellationrow(DataSet1.constellationDataTable constellations, string key, string name) {            
                var row = constellations.NewconstellationRow();
                row.key = key;
                row.name = name;
                constellations.AddconstellationRow(row);
            
        }

        static void createdsoTypes(DataSet1 db) {
            DataSet1TableAdapters.dsotypeTableAdapter adap = new DataSet1TableAdapters.dsotypeTableAdapter();
            adap.Fill(db.dsotype);
                        
            DataSet1.dsotypeDataTable dsotypes = db.dsotype;

            var results = from myRow in dsotypes.AsEnumerable()                          
                          select myRow.key;

            if(!results.Contains("ASTER")) {
                adddsotyperow(dsotypes, "ASTER", "Asterism");
            }

            if (!results.Contains("BRTNB")) {
                adddsotyperow(dsotypes, "BRTNB", "Bright Nebula");
            }

            if (!results.Contains("CL + NB")) {
                adddsotyperow(dsotypes, "CL + NB", "Cluster with Nebulosity");
            }

            if (!results.Contains("DRKNB")) {
                adddsotyperow(dsotypes, "DRKNB", "Dark Nebula");
            }

            if (!results.Contains("GALCL")) {
                adddsotyperow(dsotypes, "GALCL", "Galaxy cluster");
            }

            if (!results.Contains("GALXY")) {
                adddsotyperow(dsotypes, "GALXY", "Galaxy");
            }

            if (!results.Contains("GLOCL")) {
                adddsotyperow(dsotypes, "GLOCL", "Globular Cluster");
            }

            if (!results.Contains("GX + DN")) {
                adddsotyperow(dsotypes, "GX + DN", "Diffuse Nebula in a Galaxy");
            }

            if (!results.Contains("GX + GC")) {
                adddsotyperow(dsotypes, "GX + GC", "Globular Cluster in a Galaxy");
            }

            if (!results.Contains("G + C + N")) {
                adddsotyperow(dsotypes, "G + C + N", "Cluster with Nebulosity in a Galaxy");
            }

            if (!results.Contains("LMCCN")) {
                adddsotyperow(dsotypes, "LMCCN", "Cluster with Nebulosity in the LMC");
            }

            if (!results.Contains("LMCDN")) {
                adddsotyperow(dsotypes, "LMCDN", "Diffuse Nebula in the LMC");
            }

            if (!results.Contains("LMCGC")) {
                adddsotyperow(dsotypes, "LMCGC", "Globular Cluster in the LMC");
            }

            if (!results.Contains("LMCOC")) {
                adddsotyperow(dsotypes, "LMCOC", "Open cluster in the LMC");
            }

            if (!results.Contains("NONEX")) {
                adddsotyperow(dsotypes, "NONEX", "Nonexistent");
            }

            if (!results.Contains("OPNCL")) {
                adddsotyperow(dsotypes, "OPNCL", "Open Cluster");
            }

            if (!results.Contains("PLNNB")) {
                adddsotyperow(dsotypes, "PLNNB", "Planetary Nebula");
            }

            if (!results.Contains("SMCCN")) {
                adddsotyperow(dsotypes, "SMCCN", "Cluster with Nebulosity in the SMC");
            }

            if (!results.Contains("SMCDN")) {
                adddsotyperow(dsotypes, "SMCDN", "Diffuse Nebula in the SMC");
            }

            if (!results.Contains("SMCGC")) {
                adddsotyperow(dsotypes, "SMCGC", "Globular Cluster in the SMC");
            }

            if (!results.Contains("SMCOC")) {
                adddsotyperow(dsotypes, "SMCOC", "Open cluster in the SMC");
            }

            if (!results.Contains("SNREM")) {
                adddsotyperow(dsotypes, "SNREM", "Supernova Remnant");
            }

            if (!results.Contains("QUASR")) {
                adddsotyperow(dsotypes, "QUASR", "Quasar");
            }

            if (!results.Contains("1STAR")) {
                adddsotyperow(dsotypes, "1STAR", "1 Star");
            }

            if (!results.Contains("2STAR")) {
                adddsotyperow(dsotypes, "2STAR", "2 Stars");
            }

            if (!results.Contains("3STAR")) {
                adddsotyperow(dsotypes, "3STAR", "3 Stars");
            }

            if (!results.Contains("4STAR")) {
                adddsotyperow(dsotypes, "4STAR", "4 Stars");
            }

            if (!results.Contains("5STAR")) {
                adddsotyperow(dsotypes, "5STAR", "5 Stars");
            }



            adap.Update(dsotypes);

            /*
            ASTER Asterism
       BRTNB Bright Nebula
       CL + NB  Cluster with Nebulosity
         DRKNB  Dark Nebula
       GALCL Galaxy cluster
      GALXY  Galaxy
      GLOCL  Globular Cluster
       GX + DN  Diffuse Nebula in a Galaxy
       GX + GC  Globular Cluster in a Galaxy
       G + C + N  Cluster with Nebulosity in a Galaxy
       LMCCN Cluster with Nebulosity in the LMC
       LMCDN Diffuse Nebula in the LMC
       LMCGC Globular Cluster in the LMC
       LMCOC Open cluster in the LMC
       NONEX Nonexistent
       OPNCL Open Cluster
      PLNNB  Planetary Nebula
       SMCCN Cluster with Nebulosity in the SMC
       SMCDN Diffuse Nebula in the SMC
       SMCGC Globular Cluster in the SMC
       SMCOC Open cluster in the SMC
       SNREM Supernova Remnant
      QUASR  Quasar
      */
        }

        class catalogue {

        }


        class DSO {
            public string obj;
            public string other;
            public string type;
            public string constellation;
            public string RA;
            public string DEC;
            public string magnitude;
            public string subr;
            public string u2k;
            public string ti;
            public string size_max;
            public string size_min;
            public string PA;
            public string classification;
            public string NSTS;
            public string BRSTR;
            public string CHM;
            public string NGC_Descr;
            public string Notes;

            public override string ToString() {
                return obj;
            }

            public DSO(string[] fields) {
                obj = fields[0];
                other = fields[1];
                type = fields[2];
                constellation = fields[3];
                RA = fields[4];
                DEC = fields[5];
                magnitude = fields[6];
                subr = fields[7];
                u2k = fields[8];
                ti = fields[9];
                size_max = fields[10];
                size_min = fields[11];
                PA = fields[12];
                classification = fields[13];
                NSTS = fields[14];
                BRSTR = fields[15];
                CHM = fields[16];
                NGC_Descr = fields[17];
                Notes = fields[18];
            }
        }
    }
}
