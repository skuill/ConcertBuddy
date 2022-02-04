using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Helper
{
    [TestClass]
    public class StringHelperTest
    {
        [TestMethod]
        public void SubstringByByteLength_WithRussianDefault_AreEqual()
        {
            // Arrange
            string testData = "/track 827e1b52-1782-4e73-b8e7-a7b6939370f5 Стволок за поясок";
            string expected = "/track 827e1b52-1782-4e73-b8e7-a7b6939370f5 Стволок за ";

            // Act
            string result = StringHelper.SubstringByByteLength(testData, 64);

            //Assert
            Assert.IsTrue(string.Equals(result, expected));
        }
    }
}
