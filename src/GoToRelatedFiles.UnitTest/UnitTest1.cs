using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReSharper.Xao;

namespace GoToRelatedFiles.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private IEnumerable<string> typeNamesInFile;
        private IEnumerable<string> suffixes;

        [TestMethod]
        public void ShouldStripOffViewAndAddViewModel()
        {
            typeNamesInFile = new List<string>{"TestView"};
            suffixes = new List<string>{"ViewModel", "View"};

            CollectionAssert.Contains((ICollection) ViewModelRelatedFilesProvider.GetTypeCandidates(typeNamesInFile, suffixes), "TestViewModel");
        }

        [TestMethod]
        public void ShouldStripOffViewModelAndAddView()
        {
            typeNamesInFile = new List<string> { "TestViewModel" };
            suffixes = new List<string> { "View" , "ViewModel"};

            var typeCandidates = (ICollection)ViewModelRelatedFilesProvider.GetTypeCandidates(typeNamesInFile, suffixes);
            CollectionAssert.Contains(typeCandidates, "TestView");
            Assert.AreEqual(1, typeCandidates.Count);
        }

        [TestMethod]
        public void FindsEveryPossibleCandidate()
        {
            typeNamesInFile = new List<string> { "TestViewModel" };
            suffixes = new List<string> { "View", "ViewModel", "Test", ".feature", "Steps" };

            var typeCandidates = (ICollection)ViewModelRelatedFilesProvider.GetTypeCandidates(typeNamesInFile, suffixes);
            CollectionAssert.Contains(typeCandidates, "TestView");
            CollectionAssert.Contains(typeCandidates, "TestTest");
            CollectionAssert.Contains(typeCandidates, "Test.feature");
            CollectionAssert.Contains(typeCandidates, "TestSteps");
            Assert.AreEqual(4, typeCandidates.Count);
        }

        
    }
}
