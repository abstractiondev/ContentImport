using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheBall.Import;

namespace TestConsole
{
    class Program
    {
        // Initial tests for Document ranking scores when loading PDF texts to SQL Server 2014...
        // -- SELECT * FROM semantickeyphrasetable(FTSTest, *) ORDER BY score DESC 
        /*
        SELECT keyphrase, score 
        FROM semanticsimilaritydetailstable( 
         FTSTest, 
         FTSContent, 4, 
         FTSContent, 5);
         */
         /*
        SELECT ID, Filename, keyphrase, score FROM semantickeyphrasetable(FTSTest, *) 

        INNER JOIN FTSTest ON ID = document_key

        WHERE ID = 4 or ID = 5 ORDER BY ID, score DESC
        */
        /*
        SELECT ID, Filename, Score 

        FROM semanticsimilaritytable(FTSTest, *, 4) 

        INNER JOIN FTSTest ON ID = matched_document_key

        ORDER BY score DESC
        */


        static void Main(string[] args)
        {

            string connStr = File.ReadAllText(@"c:\tmp\PDFsToImport\testconnstr.txt");
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            SqlCommand cmd = new SqlCommand("delete from FTSTest", conn);
            cmd.ExecuteNonQuery();

            string directoryToLookFor = @"c:\tmp\PDFsToImport\";
            string[] allPDFs = Directory.GetFiles(directoryToLookFor, "*.pdf");
            foreach (string fullName in allPDFs)
            {
                string fileName = Path.GetFileName(fullName);
                byte[] fileData = File.ReadAllBytes(fullName);
                var result = PDFImport.GetPDFAsText(fileData);
                SqlCommand insertPDF = new SqlCommand("insert into FTSTest(Filename, FTSContent) values(@Filename, @FTSContent)", conn);
                insertPDF.Parameters.Add("@Filename", fileName);
                insertPDF.Parameters.Add("@FTSContent", result);
                insertPDF.ExecuteNonQuery();
            }
            conn.Close();

        }
    }
}
