// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;

namespace Dacs7
{
    public enum ErrorClass : byte
    {
        [Description("No error")]
        None = 0x00,

        [Description("Application relationship")]
        ApplicationRelationship = 0x81,

        [Description("Object definition")]
        ObjectDefinition = 0x82,

        [Description("No resources available")]
        Resources = 0x83,

        [Description("Error on service processing")]
        Service = 0x84,

        [Description("Error on supplies")]
        Supplies = 0x85,

        [Description("Access error")]
        Access = 0x87
    }



}