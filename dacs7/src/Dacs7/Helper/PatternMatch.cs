using System;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7.Helper
{
    public class PatternMatch<T> where T : IComparable
    {
        public class Result
        {
            public int PatternLen { get; internal set; }
            public int MatchPos { get; internal set; }
            public int MatchLen { get; internal set; }
            public bool FullMatch { get { return MatchLen == PatternLen && MatchPos >= 0; } }
            public bool PartialMatch { get { return MatchLen < PatternLen && MatchPos >= 0; } }
            public bool NoMatch { get { return MatchPos < 0; } }
            public Result(int aPatternLen, int aMatchPos, int aMatchLen)
            {
                PatternLen = aPatternLen;
                MatchPos = aMatchPos;
                MatchLen = aMatchLen;
            }
            internal Result(int aPatternLen)
            {
                PatternLen = aPatternLen;
                MatchPos = -1;
                MatchLen = 0;
            }
            internal void Reset()
            {
                MatchPos = -1;
                MatchLen = 0;
            }
        }
        public static Result MatchOrMatchPartiallyAtEnd(IEnumerable<T> aCollection, IEnumerable<T> aPattern, int offset = 0)
        {
            var enumerable = aPattern as T[] ?? aPattern.ToArray();
            var collectionIndex = offset - 1;
            var matchLength = 0;
            var patternLength = enumerable.Length;
            var matchPosition = -1;
            foreach (var element in aCollection.Skip(offset))
            {
                collectionIndex++;
                if (element.CompareTo(enumerable[matchLength]) == 0)
                {
                    if (matchLength == 0)
                        matchPosition = collectionIndex;

                    matchLength++;
                    if (matchLength == patternLength && matchPosition >= 0)
                        return new Result(patternLength, matchPosition, matchLength); // READY!
                }
                else if (matchLength > 0)
                {
                    // restart comparison
                    matchLength = 0;
                    matchPosition = -1;
                }
            }

            return new Result(patternLength, matchPosition, matchLength);
        }
    }

    public class StringMatch
    {
        public class Result
        {
            public int PatternLen { get; internal set; }
            public int MatchPos { get; internal set; }
            public int MatchLen { get; internal set; }
            public bool FullMatch { get { return MatchLen == PatternLen && MatchPos >= 0; } }
            public bool PartialMatch { get { return MatchLen < PatternLen && MatchPos >= 0; } }
            public bool NoMatch { get { return MatchPos < 0; } }
            public Result(int aPatternLen, int aMatchPos, int aMatchLen)
            {
                PatternLen = aPatternLen;
                MatchPos = aMatchPos;
                MatchLen = aMatchLen;
            }
            internal Result(int aPatternLen)
            {
                PatternLen = aPatternLen;
                MatchPos = -1;
                MatchLen = 0;
            }
        }

        public static Result MatchOrMatchPartiallyAtEnd(string aString, string aPattern)
        {
            var result = new Result(aPattern.Length)
            {
                MatchPos = aString.IndexOf(aPattern, StringComparison.Ordinal)
            };

            if (result.MatchPos >= 0)
            {
                result.MatchLen = aPattern.Length;
                return result;
            }


            // shorten the pattern by one character or to the length of the searched string (whichever is smaller)
            aPattern = aPattern.Substring(0, Math.Min(aPattern.Length - 1, aString.Length));

            while (aPattern.Length > 0)
            {
                // Check if it partially fits at the end ...
                // so if more chars get appended to aString, it might match then.
                if (aString.Substring(aString.Length - aPattern.Length, aPattern.Length) == aPattern)
                {
                    result.MatchPos = aString.Length - aPattern.Length;
                    result.MatchLen = aPattern.Length;
                    return result;
                }

                // again shorten the pattern by one character
                aPattern = aPattern.Substring(0, aPattern.Length - 1);
            }

            return result;
        }
    }

}
