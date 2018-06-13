using Dacs7.Domain;
using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7TagParserException : Exception
    {
        private readonly TagParserState _parseArea;

        public string Tag { get; private set; }
        public string ParseData { get; private set; }

        internal Dacs7TagParserException(TagParserState parseArea, string area, string tag) :
            base($"Could not extract {Enum.GetName(typeof(TagParserState), parseArea)} from data '{area}'. Full tag was '{tag}'.")
        {
            _parseArea = parseArea;
            ParseData = area;
            Tag = tag;
        }

    }
}