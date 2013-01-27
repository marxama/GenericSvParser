using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Marxama
{
    /// <summary>
    /// Helper class for GenericSvParser. Exposes the static GetSeparator method - see its documentation
    /// for more information.
    /// </summary>
    class SeparatorExtractor
    {
        /// <summary>
        /// Will look for non-alphanumeric separators only. Will return the separator candidate
        /// which occurs most frequently on each line, with the same number of occurences on each 
        /// line. If multiple candidates exist, the longest one will be returned. If multiple choices
        /// still exist, an AmbiguityException is thrown, unless specified to be suppressed, in which
        /// case any of the final potential separators may be chosen.
        /// </summary>
        /// <param name="lines">An array of csv lines.</param>
        /// <param name="useHeaders">Whether or not to use headers. Is used within the algorithm.</param>
        /// <param name="suppressAmbiguityExceptions">Whether or not to suppress AmbiguityExceptions.</param>
        /// <returns>The best candidate to use as a separator for each line.</returns>
        public static string GetSeparator(string[] lines, bool useHeaders, bool suppressAmbiguityExceptions)
        {
            /* potentialSeparators contains lists of potential separators, extracted from the first line. 
             * The candidates within each list all occur the same number of times. The lists are sorted
             * on number of occurences, with most frequent candidates first.
             */
            List<List<string>> potentialSeparators = getPotentialSeparators(lines, useHeaders);

            foreach (List<string> separators in potentialSeparators)
            {
                // Filter out the separators which do not occur the same number of times on all lines
                List<string> validSeparators = separators.FindAll((str) =>
                {
                    return substrOccursXTimesInEachLine(lines, str);
                });

                if (validSeparators.Count == 1)
                {
                    return validSeparators[0];
                }
                else if (validSeparators.Count > 1)
                {
                    string prioritizedSeparator = getPrioritizedSeparator(validSeparators);
                    if (prioritizedSeparator != null)
                    {
                        return prioritizedSeparator;
                    }
                    else if (suppressAmbiguityExceptions)
                    {
                        return validSeparators[0];
                    }
                    else
                    {
                        throw new AmbiguityException("Could not determine separator.");
                    }
                }
                /* ...and if there where no valid separators found, continue to the next list of potential separators,
                 * occuring less frequently than the current ones */
            }

            throw new Exception("Unable to determine separator");
        }

        /// <summary>
        /// Given a list of potential separators occuring the same number of times, returns
        /// the best choice, or null if a best choice could not be determined.
        /// </summary>
        /// <remarks>
        /// Currently simply returns the longest string withing potentialSeparators, unless
        /// there are several separators of that same length.
        /// </remarks>
        /// <param name="potentialSeparators"></param>
        /// <returns></returns>
        private static string getPrioritizedSeparator(List<string> potentialSeparators)
        {
            // Sort the separators so the longest one is first
            potentialSeparators.Sort((a, b) => -a.Length.CompareTo(b.Length));
            List<string> separatorsWithLongestLength = potentialSeparators.FindAll((str) => str.Length == potentialSeparators[0].Length);
            if (separatorsWithLongestLength.Count == 1)
            {
                return separatorsWithLongestLength[0];
            }
            else
            {
                /* Here, we could specify if we want to prioritize, for example, "," higher than anything else, ";" second, 
                 * and so on. This is left for now, for generality. */
                return null;
            }
        }

        /// <summary>
        /// Returns a list containing lists of potential separators. The potential separators in each list 
        /// occur the same number of times, and the lists are sorted in descending order on frequency.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="useHeaders"></param>
        /// <returns></returns>
        private static List<List<string>> getPotentialSeparators(string[] lines, bool useHeaders)
        {
            string firstLine = lines[0];
            string[] stuffBetweenAlphaNumerics = Regex.Split(firstLine, "\\w+");

            string[] allPotentialSeparators = getAllSubstrings(stuffBetweenAlphaNumerics);

            /* if we are using a headers line, then we can filter out the potential separators which 
             * would give duplicate header names */
            if (useHeaders) {
                allPotentialSeparators = allPotentialSeparators.ToList().FindAll((separator) =>
                {
                    string[] headers = lines[0].Split(new string[] { separator }, StringSplitOptions.None);
                    return !containsDuplicates(headers);
                }).ToArray();
            }

            /* potentialSeparatorsWithNumberOfOccurences will map the potential separators to the number 
             * of times they occur in the first line. */
            Dictionary<string, int> potentialSeparatorsWithNumberOfOccurences = new Dictionary<string, int>();
            foreach (string separator in allPotentialSeparators)
            {
                if (!potentialSeparatorsWithNumberOfOccurences.ContainsKey(separator))
                {
                    potentialSeparatorsWithNumberOfOccurences.Add(separator, 1);
                }
                else
                {
                    potentialSeparatorsWithNumberOfOccurences[separator]++;
                }
            }

            int currentCount = -1;
            List<List<string>> result = new List<List<string>>();
            foreach (KeyValuePair<string, int> pair in potentialSeparatorsWithNumberOfOccurences.OrderBy((key => -key.Value)))
            {
                int count = pair.Value;
                if (count != currentCount)
                {
                    result.Add(new List<string>());
                    currentCount = count;
                }
                result[result.Count - 1].Add(pair.Key); // Add the current potential separator to the last list
            }

            return result;
        }

        /// <summary>
        /// Determines whether there are any duplicates in the given array.
        /// </summary>
        /// <param name="strs"></param>
        /// <returns></returns>
        private static bool containsDuplicates(string[] strs)
        {
            Array.Sort(strs);
            for (int i = 0; i < strs.Length - 1; i++)
            {
                if (strs[i].Equals(strs[i + 1]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a list of all substrings of each string in the given array.
        /// </summary>
        /// <remarks>
        /// For example, if the array contained the string
        /// #,;
        /// then the following substrings would be yielded
        /// #,;  #,  #  ,;  ,  ;
        /// </remarks>
        /// <param name="strings"></param>
        /// <returns></returns>
        private static string[] getAllSubstrings(string[] strings)
        {
            List<string> result = new List<string>();

            foreach (string str in strings)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    for (int j = i; j < str.Length; j++)
                    {
                        result.Add(str.Substring(i, j - i + 1));
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns true if substr occurs the same number of times in each string in lines, otherwise false.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="substr"></param>
        /// <returns></returns>
        private static bool substrOccursXTimesInEachLine(string[] lines, string substr)
        {
            int occurencesForFirstLine = -1;
            
            foreach (string line in lines)
            {
                int occurencesForCurrentLine = line.Select((c, i) => line.Substring(i)).Count(sub => sub.StartsWith(substr));
                if (occurencesForFirstLine == -1)
                {
                    occurencesForFirstLine = occurencesForCurrentLine;
                }
                else if (occurencesForCurrentLine != occurencesForFirstLine)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
