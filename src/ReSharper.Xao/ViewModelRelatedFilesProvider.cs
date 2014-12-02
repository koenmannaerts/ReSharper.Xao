﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation.GoToRelated;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharper.Xao
{
    // Comment the RelatedFilesProvider attribute before opening the file.  Its presence 
    // somehow causes the editor to go nuts.
    [RelatedFilesProvider(typeof(KnownProjectFileType))]
    public class ViewModelRelatedFilesProvider : IRelatedFilesProvider
    {
        private static readonly string[] ViewSuffixes = { "View", "Flyout", "UserControl", "Page", "ViewModel", "Test" };

        public IEnumerable<JetTuple<IProjectFile, string, IProjectFile>> GetRelatedFiles(IProjectFile projectFile)
        {
            var typeNamesInFile = GetTypeNamesDefinedInFile(projectFile).ToList();

            var candidateTypeNames = GetTypeCandidates(typeNamesInFile, ViewSuffixes);

            // Look for the candidate types throught the solution.
            var solution = projectFile.GetSolution();
            var candidateTypes = new List<IClrDeclaredElement>();
            foreach (var candidateTypeName in candidateTypeNames)
            {
                var types = FindType(solution, candidateTypeName);
                candidateTypes.AddRange(types);
            }

            // Get the source files for each of the candidate types.
            var sourceFiles = new List<IPsiSourceFile>();
            foreach (var type in candidateTypes)
            {
                var sourceFilesForCandidateType = type.GetSourceFiles();
                sourceFiles.AddRange(sourceFilesForCandidateType);
            }

            var elementCollector = new RecursiveElementCollector<ITypeDeclaration>();
            foreach (var psiSourceFile in sourceFiles)
                foreach (var file in psiSourceFile.EnumerateDominantPsiFiles())
                    elementCollector.ProcessElement(file);

            var elements = elementCollector.GetResults();
            IEnumerable<IProjectFile> projectFiles = elements.Select(declaration => declaration.GetSourceFile().ToProjectFile());

            var rval = new List<JetTuple<IProjectFile, string, IProjectFile>>();
            foreach (var file in projectFiles.OfType<ProjectFileImpl>().Distinct(pf => pf.Location.FullPath))
            {
                // Remove all extensions (e.g.: .xaml.cs).
                var fn = file.Name;
                var dotPos = fn.IndexOf('.');
                if (dotPos != -1)
                    fn = fn.Substring(0, dotPos);
                var display = fn.EndsWith("ViewModel") ? "ViewModel" : "View";
                
                var tuple = JetTuple.Of((IProjectFile)file, display, projectFile);
                
                rval.Add(tuple);
            }

            return rval;
        }

        public static IEnumerable<string> GetTypeCandidates(IEnumerable<string> typeNamesInFile, IEnumerable<string> suffixes)
        {
            var candidates = new List<string>();
            var orderedEnumerable = suffixes.OrderByDescending(x => x.Length).ToList();

            foreach (var typeName in typeNamesInFile)
            {
                var suffixesToAdd = orderedEnumerable.Select(x => x).ToList();
                foreach (var suffix in orderedEnumerable)
                {
                    if (typeName.EndsWith(suffix))
                    {
                        suffixesToAdd.Remove(suffix);
                        string trim = typeName.Substring(0, typeName.LastIndexOf(suffix));
                        foreach (var suffixToAdd in suffixesToAdd)
                        {
                            candidates.Add(trim + suffixToAdd);
                        }    
                    }
                    
                }
                
            }
            

            //// For each type name in the file, create a list of candidates.
            //foreach (var typeName in typeNamesInFile)
            //{
            //    // If a view model...
            //    if (typeName.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
            //    {
            //        // Remove ViewModel from end and add all the possible suffixes.
            //        var baseName = typeName.Substring(0, typeName.Length - 9);
            //        foreach (var suffix in ViewSuffixes)
            //        {
            //            var candidate = baseName + suffix;
            //            candidates.Add(candidate);
            //        }

            //        // Add base if it ends in one of the view suffixes.
            //        foreach (var suffix in ViewSuffixes)
            //            if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            //            {
            //                candidates.Add(baseName);
            //                break;
            //            }
            //    }

            //    foreach (var suffix in ViewSuffixes)
            //    {
            //        if (typeName.EndsWith(suffix))
            //        {
            //            // Remove suffix and add ViewModel.
            //            var baseName = typeName.Substring(0, typeName.Length - suffix.Length);
            //            var candidate = baseName + "ViewModel";
            //            candidates.Add(candidate);

            //            // Just add ViewModel
            //            candidate = typeName + "ViewModel";
            //            candidates.Add(candidate);
            //        }
            //    }
            //}

            return candidates;
        }

        private IEnumerable<string> GetTypeNamesDefinedInFile(IProjectFile projectFile)
        {
            IPsiSourceFile psiSourceFile = projectFile.ToSourceFile();
            if (psiSourceFile == null)
                return EmptyList<string>.InstanceList;

            return psiSourceFile.GetPsiServices().Symbols.GetTypesAndNamespacesInFile(psiSourceFile)
                                .OfType<ITypeElement>()
                                .Select(element => element.ShortName);
        }

        private static List<IClrDeclaredElement> FindType(ISolution solution, string typeToFind)
        {
            ISymbolScope declarationsCache = solution.GetPsiServices().Symbols
                .GetSymbolScope(LibrarySymbolScope.FULL, context: UniversalModuleReferenceContext.Instance, caseSensitive: false);

            List<IClrDeclaredElement> results = declarationsCache.GetElementsByShortName(typeToFind).ToList();
            return results;
        }
    }
}