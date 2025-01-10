using statrecovery.Models;
using statrecovery.Utils;

namespace statrecoveryTests.Utils
{
    [TestClass()]
    public class DatabaseManagerTests
    {
        private static Database ExpectedDatabase()
        {
            var db = new Database()
                .AddPdf("zip1", new("pdf 1 zip 1", new DateTime(2024, 12, 31, 10, 25, 33), "11"))
                .AddPdf("zip1", new("pdf 1 zip 2", new DateTime(2025, 1, 1, 15, 30, 10), "12"))
                .AddPdf("zip2", new("pdf 2 zip 1", new DateTime(2024, 10, 31, 13, 5, 11), "21"))
                .AddPdf("zip2", new("pdf 2 zip 2", new DateTime(2025, 1, 1, 10, 30, 45), "22"));

            return db;
        }

        private static readonly string[] expectedText = [ "zip1\t2024-12-31 10:25:33Z|pdf 1 zip 1|11\\2025-01-01 15:30:10Z|pdf 1 zip 2|12",
                                                          "zip2\t2024-10-31 13:05:11Z|pdf 2 zip 1|21\\2025-01-01 10:30:45Z|pdf 2 zip 2|22" ];

        private static readonly string bufferFileName = @".\test.db";

        [TestMethod()]
        public void DatabaseManager_SaveToFile_Success()

        {
            var db = ExpectedDatabase();
            DatabaseManager.SaveToFile(db, bufferFileName);

            var currentText = File.ReadAllText(@bufferFileName).TrimEnd('\r', '\n');

            Assert.IsTrue(currentText == $"{expectedText[0]}\r\n{expectedText[1]}"
                || currentText == $"{expectedText[1]}\r\n{expectedText[0]}");
        }

        [TestMethod()]
        public void DatabaseManager_LoadFromFile_Success()
        {
            File.WriteAllText(bufferFileName, string.Join("\r\n", expectedText));
            var currentDb = DatabaseManager.LoadFromFile(bufferFileName);

            var expectedDb = ExpectedDatabase();

            Assert.AreEqual(@expectedDb.Keys.Count, currentDb.Keys.Count);
            Assert.IsTrue(@expectedDb.Keys.All(currentDb.ContainsKey));
            Assert.IsTrue(currentDb.Keys.All(@expectedDb.ContainsKey));
            Assert.IsTrue(@expectedDb.Keys.All(key => @expectedDb[key].All(expectedPdf => currentDb[key].Any(currentPdf => currentPdf.Equals(expectedPdf)))));
            Assert.IsTrue(currentDb.Keys.All(key => currentDb[key].All(currentPdf => currentDb[key].Any(expectedPdf => expectedPdf.Equals(currentPdf)))));
        }
    }
}