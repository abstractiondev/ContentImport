using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        class DBFieldTextData
        {
            public string FieldName;
            public string TextContent;
        }

        static void Main(string[] args)
        {

            string connStr = File.ReadAllText(@"c:\tmp\PDFsToImport\testconnstr.txt");
            string regexStr = File.ReadAllText(@"c:\tmp\PDFsToImport\sectionsplit_regexps.txt");
            Regex regex = new Regex(regexStr, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            //SqlCommand cmd = new SqlCommand("delete from FTSTest", conn);
            //cmd.ExecuteNonQuery();

            string directoryToLookFor = @"c:\tmp\PDFsToImport\";
            string[] allPDFs = Directory.GetFiles(directoryToLookFor, "*.pdf");
            foreach (string fullName in allPDFs)
            {
                string fileName = Path.GetFileName(fullName);
                SqlCommand queryExisting = new SqlCommand("select count(*) from FTSTest where Filename = @Filename", conn);
                queryExisting.Parameters.Add("@Filename", fileName);
                var existingCount = (int)queryExisting.ExecuteScalar();
                if (existingCount > 0)
                    continue;
                Console.WriteLine("Converting: " + fileName);
                byte[] fileData = File.ReadAllBytes(fullName);
                var result = PDFImport.GetPDFAsText(fileData);
                var matches = regex.Matches(result);
                List<Match> matchList = new List<Match>();
                foreach (Match match in matches)
                {
                    matchList.Add(match);
                }
                DBFieldTextData[] fieldSplits = GetSections(matchList, regex, result);
                int[] locations = matchList.Select(m => m.Index).ToArray();
                string txtName = fullName.Replace(".pdf", ".txt");
                File.WriteAllText(txtName, result);
                Console.WriteLine("Uploading: " + fileName);
                long id = getIDFromFilenameOrIncrementalFromDB(fileName, conn);
                SqlCommand insertPDF = new SqlCommand("insert into FTSTest(ID, Filename, FTSContent) OUTPUT INSERTED.ID values(@ID, @Filename, @FTSContent)", conn);
                insertPDF.Parameters.Add("@ID", id);
                insertPDF.Parameters.Add("@Filename", fileName);
                insertPDF.Parameters.Add("@FTSContent", result);
                //insertPDF.ExecuteNonQuery();
                long lastID = (long) insertPDF.ExecuteScalar();
                foreach (var fieldSplit in fieldSplits)
                {
                    SqlCommand updateCmd = new SqlCommand(String.Format("Update FTSTest set {0} = @content where id = @ID", fieldSplit.FieldName), conn);
                    updateCmd.Parameters.Add("@ID", lastID);
                    updateCmd.Parameters.Add("@content", fieldSplit.TextContent);
                    updateCmd.ExecuteNonQuery();
                }

            }
            conn.Close();

        }

        private static long getIDFromFilenameOrIncrementalFromDB(string fileName, SqlConnection conn)
        {
            string candidateIDStr = Path.GetFileNameWithoutExtension(fileName);
            try
            {
                return Convert.ToInt64(candidateIDStr);
            }
            catch (FormatException)
            {
                SqlCommand command = new SqlCommand("select max(id) + 1 from FTSTest", conn);
                long id = (long) command.ExecuteScalar();
                if (id < 1000000000)
                    id = 1000000000;
                return id;
            }
        }

        private static DBFieldTextData[] GetSections(List<Match> matchList, Regex regex, string fullContent)
        {
            int contentMinimumLength = 500;
            List<DBFieldTextData> result = new List<DBFieldTextData>();
            if(matchList.Count != 5 && matchList.Count != 10)
                return new DBFieldTextData[0];
            var groupNames = regex.GetGroupNames().Skip(1).ToArray();
            for (int i = 0; i < matchList.Count - 1; i++)
            {
                Match currMatch = matchList[i];
                string currGroupName = null;
                foreach (var candGroupName in groupNames)
                {
                    var candidateGroup = currMatch.Groups[candGroupName];
                    if (candidateGroup != null && candidateGroup.Success)
                    {
                        currGroupName = candGroupName;
                        break;
                    }
                        
                }
                if (currGroupName.EndsWith("End"))
                    continue;
                Match nextMatch = matchList[i + 1];
                int length = nextMatch.Index - currMatch.Index;
                if (length < 500)
                    continue;
                string sectionContent = fullContent.Substring(currMatch.Index, length);
                result.Add(new DBFieldTextData
                    {
                        FieldName = currGroupName,
                        TextContent = sectionContent
                    });
            }
            return result.ToArray();
        }
    }
}

