// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Communication
{
    public interface IConfiguration
    {
        int ReceiveBufferSize { get; set; }
        int AutoconnectTime { get; set; }
    }
}
