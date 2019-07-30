// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Dacs7
{
    [Serializable]
    public class Dacs7TagParserException : Exception, ISerializable
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

        protected Dacs7TagParserException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            _parseArea = (TagParserState)serializationInfo.GetValue("_parseArea", typeof(TagParserState));
            Tag = (string)serializationInfo.GetValue("Tag", typeof(string));
            ParseData = (string)serializationInfo.GetValue("ParseData", typeof(string));
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_parseArea", _parseArea);
            info.AddValue("Tag", Tag);
            info.AddValue("ParseData", ParseData);
        }

        public Dacs7TagParserException()
        {
        }

        public Dacs7TagParserException(string message) : base(message)
        {
        }

        public Dacs7TagParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}