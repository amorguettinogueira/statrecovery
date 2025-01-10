using statrecovery.Models;

namespace statrecovery.Utils.Tests
{
    [TestClass()]
    public class ProcessTests
    {
        private static Dictionary<string, string> ExpectedDictionary()
        {
            var dict = new Dictionary<string, string>
            {
                {"60300166_17485212_20211027_030112177.pdf","4269173231"},
                {"45348015_1_3108869_Walmart_claim.pdf","6304175188"},
                {"45348015_2_3108869_Walmart_claim.pdf","6304175188"},
                {"45348015_3_3108869_Walmart_claim.pdf","6304175188"},
                {"_1638456165561.pdf","6304175188"},
            };

            return dict;
        }

        private static string csvMockUpData = @"Id~Claim Number~Claim Date~Open Amount~Original Amount~Status~Customer Name~AR Reason Code~Customer Reason Code~Attachment List~Check Number~Check Date~Comments~Days Outstanding~Division~PO Number~Brand~Merge Status~Unresolved Amount~Document Type~Document Date~Original Customer~Location~Customer Location~Create Date~Load Id~Carrier Name~Invoice Store Number
27762713~~~2760.00~2760.00~Wait~WALMART~JINA4~~/s3hrc-cpa-prod/136/2021/10/24/acctDoc/arextract//input/60300166_17485212_20211027_030112177.pdf~~~needs claim number~5~40~4269173231~640~No~2760.00~CBK~2021-11-29~01111~KEV~~2021-12-01~001509068~~8234
27762716~45348015~2021-11-17~2825.70~2825.70~Wait~WALMART~JINA4~24~/s3hrc-cpa-prod/136/2021/11/14/Walmart/45348015_1_3108869_Walmart_claim.pdf,/s3hrc-cpa-prod/136/2021/11/14/Walmart/45348015_2_3108869_Walmart_claim.pdf,/s3hrc-cpa-prod/136/2021/11/14/Walmart/45348015_3_3108869_Walmart_claim.pdf,/s3hrc-dms-prod/136/2021/11/28/deductions/attachments/_1638456165561.pdf~001714846~2021-11-18~12/6/21~5~40~6304175188~671~Yes~2825.70~CBK~2021-11-29~00004~NEWDC~05-8011~2021-12-01~~~00355";

        private static readonly string bufferFileName = @".\test.csv";

        [TestMethod()]
        public void Process_LoadCsvs_Success()
        {
            File.WriteAllText(bufferFileName, csvMockUpData);

            var currentDict = Process.LoadCsvs([bufferFileName], CancellationToken.None);
            var expectedDict = ExpectedDictionary();

            Assert.AreEqual(expectedDict.Keys.Count, currentDict.Keys.Count);
            Assert.IsTrue(expectedDict.Keys.All(currentDict.ContainsKey));
            Assert.IsTrue(currentDict.Keys.All(expectedDict.ContainsKey));
            Assert.IsTrue(expectedDict.Keys.All(key => expectedDict[key] == currentDict[key]));
            Assert.IsTrue(currentDict.Keys.All(key => currentDict[key] == expectedDict[key]));
        }
    }
}