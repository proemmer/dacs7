// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Communication
{
    public interface ISocketConfiguration : IConfiguration
    {
        string Hostname { get; set; }
        int ServiceName { get; set; }
        string NetworkAdapter { get; set; }
        bool KeepAlive { get; set; }
    }
}