﻿using LyricsScraper.Abstract;
using LyricsScraper.AZLyrics;
using LyricsScraper.Common;
using LyricsScraper.Test.TestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;

namespace LyricsScraper.Test.AZLyrics
{
    [TestClass]
    public class AZLyricsGetterTest
    {
        private const string TEST_DATA_PATH = "AZLyrics\\test_data.json";
        private List<LyricsTestData> _testDataCollection;

        [TestInitialize]
        public void TestInitialize()
        {
            _testDataCollection = Serializer.Deseialize<List<LyricsTestData>>(TEST_DATA_PATH);
        }

        [TestMethod]
        public void SearchLyric_MockWebClient_AreEqual()
        {

            foreach (var testData in _testDataCollection)
            {
                // Arrange
                var mockWebClient = new Mock<IWebClient>();
                mockWebClient.Setup(x => x.Load(It.IsAny<Uri>())).Returns(testData.LyricPageData);

                var lyricsGetter = new AZLyricsGetter(null, new AZLyricsParser(), new HtmlAgilityWebClient());
                lyricsGetter.WithWebClient(mockWebClient.Object);

                // Act
                var lyric = !string.IsNullOrEmpty(testData.SongUri) 
                    ? lyricsGetter.SearchLyric(new Uri(testData.SongUri)) 
                    : lyricsGetter.SearchLyric(testData.ArtistName, testData.SongName);

                // Result
                Assert.AreEqual(testData.LyricResultData, lyric);
            }
        }
    }
}
