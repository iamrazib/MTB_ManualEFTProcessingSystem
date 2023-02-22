using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualEFTProcessingSystem.DBUtility
{
    class ConnectionInfo
    {
        /*
        //-- UAT --------------
        static string connectionStringNrbWork = "7yd4MP3VGCgFvFyV4t81hL13K9EFP3ubA88Gd4uAdysuaCtpV0CK3mj4X0YfgCkRE4ad1mnn1ECxiq1QgI1r7tVhQ9e9nIP1HUv8HI7YY2PCwHYvHdGPT5Shgbz2/MxI8B8nRwiDXQ0rZOnn4cXKkL1trbjnruYd8a2arAPjkD1OncSTVmUELnrFu9W7nov/f0+yvp1UEAMpC8xgc37ONA==";
        //Data Source=192.168.81.53;Initial Catalog=NRBWork;User ID=sa;Password=mtbadmin

        static string connectionStringRemitDbLv = "7yd4MP3VGCgFvFyV4t81hL13K9EFP3ubA88Gd4uAdysuaCtpV0CK3mj4X0YfgCkRWyTtX+wabeuA12JuF58oWAsgmFsSO/UVFyi7oVGX3PbdnZGrvTzzRUsx/MnWr9OWhHSb4Hr+5L42LtSxkOADZVH31epgPuzO++JvrgyRmWATzl8x8ZuxjdJtD9MofW4bP1R5FBia6I4uL32b6z9s1O5/LxLfOH5BciCHbjPrtIY=";
        //Data Source=192.168.81.53; Initial Catalog=RemittanceDB; User ID=sa; Password=mtbadmin

        static string connectionStringDR = "7yd4MP3VGCgFvFyV4t81hL13K9EFP3ubA88Gd4uAdysuaCtpV0CK3mj4X0YfgCkRWyTtX+wabeuA12JuF58oWAsgmFsSO/UVFyi7oVGX3PbdnZGrvTzzRUsx/MnWr9OWhHSb4Hr+5L42LtSxkOADZVH31epgPuzO++JvrgyRmWATzl8x8ZuxjdJtD9MofW4bP1R5FBia6I4uL32b6z9s1O5/LxLfOH5BciCHbjPrtIY=";
        //Data Source=192.168.81.53; Initial Catalog=RemittanceDB; User ID=sa; Password=mtbadmin
        //------------------------------------
        */

        
        //--- Live -------------
        static string connectionStringNrbWork = "7yd4MP3VGCgFvFyV4t81hL13K9EFP3ubmIfV6arMv+BwEcytVleK79E65XBpWfELZ9kv1vYl3SSGBDiZUxFvCZXWTbb3yIdh39Kk7C+7mG0RBYFvZrFwKxiGwe82cAM2CvRj6bB2aok0UlP/RKm2RafoU6fBX3I42TZzCj7hH585++AECeXTFxrFYHxALYsUBjsaxa18AIbW3uRXv64WjhVHIiMnx5eT";
        //string dbcon = "Data Source=10.45.10.106;Initial Catalog=NRBWork;User ID=nrbwork;Password=Mtb@1234";

        //RemittanceDB lv conn
        static string connectionStringRemitDbLv = "7yd4MP3VGCgFvFyV4t81hL13K9EFP3ubmIfV6arMv+BwEcytVleK79E65XBpWfELZ9kv1vYl3SSGBDiZUxFvCZXWTbb3yIdh39Kk7C+7mG1/dbaPxIfVPpoJ3i/CopIKvi/j+t6eAxgpW0DHT5mPAsqCKIU28rJq/J052vVDsFaBFZHcMmKVkLSF/I9f2NFE98qlOaexe0+QzLDZf5AIj/MNZOurr5stg1f5Z8cBZcuWho7DSzANa4ZsOJuNWI2OW0HrKagfq2zRvxWycJGpJx0e3Wv6AYyxy5LBAovYRNo=";
        // Data Source=10.45.10.106;Initial Catalog=RemittanceDB;User ID=remittanceapiUser;Password=Rem!ttanceApiUser!@34#

        static string connectionStringDR = "7yd4MP3VGCgFvFyV4t81hL13K9EFP3ubmIfV6arMv+BtIoCJhQveQnuox1ealjBDqCjmFv/Weq2v8uSCSn8HqMkx+RjWcPb2wni2vf0pOO1seBVBAUKjFAqCOyOngWkL2Zw4c1y7lpzeqBL69eafshKb/EYq+tE1X10263q1wp4pbnc2P5YB9z6AUfjdOLL/kcFnxJql5faLBTchNt9b9T7oMfTiZ4og+kzzz+pDlec/5WTSLXGNJMZdopKJH0uXYlYeANPOqU5z5aDSYVqj8S4kpYVBfwplTXh/xA9Agno=";
        // Data Source=10.46.10.106;Initial Catalog=RemittanceDB;User ID=remittanceapiUser;Password=R3mitt@nc3@p!us3r@321
        //------------------------------------
        

        public string getNrbWorkConnString()
        {
            return Utility.DecryptString(connectionStringNrbWork);
        }

        public string getConnStringDR()
        {
            return Utility.DecryptString(connectionStringDR);
            //return Utility.DecryptString(connectionStringRemitDbLv);
        }

        public string getConnStringRemitDbLv()
        {
            return Utility.DecryptString(connectionStringRemitDbLv);
        }

    }
}
