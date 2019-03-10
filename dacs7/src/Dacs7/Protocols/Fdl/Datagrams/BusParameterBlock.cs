namespace Dacs7.Protocols.Fdl
{

    internal sealed class Ident
    {
        public byte[] ReservedHeader { get; set; } = new byte[8];                    //reserved for FDL !!!!!                                               
        public byte[] IdentInfo { get; set; } = new byte[202];
        public byte ResponseTelegramLength { get; set; }               //reserved for FDL !!!!!                                               
    }

    internal sealed class BusParameterBlock
    {
        public byte HighestStationAddress { get; set; }                  //highest station-address                                              
                                                                         //range of values:  2 ... 126                                          
        public byte FdlAddress { get; set; }                   //FDL-address of this station                                          
                                                               //range of values:  0 ... 126                                          
                                                               //# ifdef M_DOS
                                                               //        enum station_type    station_type { get; set; }          //active, passive                                                      
                                                               //    enum baud_rate       baud_rate { get; set; }             //transmission rate                                                    
                                                               //    enum redundancy      medium_red { get; set; }            //availability of redundant media                                      
                                                               //#else
        public short StationType { get; set; }          //active, passive                                                      
        public short BaudRate { get; set; }             //transmission rate                                                    
        public short MediumRed { get; set; }            //availability of redundant media                                      

        public ushort RetryCtr { get; set; }            //retry-number of requestor, if no reaction of responder               
                                                        //range of values:  1 ... 8                                            
        public byte DefaultSap { get; set; }          //Default SAP if no address-extension is used                          
                                                      //range of values:  2 ... 62                                           
        public byte NetworkConnectionSap { get; set; }//number of sap for network-connection (only for network-connections) 
                                                      //range of values:  2 ... 62                                           
        public ushort TimeSlot { get; set; }                  //SLOT-time:                                                           
                                                              //range of values:  2 exp 0 ... (2 exp 16) - 1   BIT-times             
        public ushort TransmitterFallTime { get; set; }                 //Transmitter-Fall-Time / Repeater-Switch-Time:                        
                                                                        //range of values:  0 ... (2 exp 8) - 1 BIT-times                      
        public ushort SetupTime { get; set; }                 //setup-time                                                           
                                                              //range of values:  0 ... (2 exp 8) - 1 BIT-times                      
        public ushort SmallestStationDelayTime { get; set; }             //smallest STATION-DELAY-time:                                         
                                                                         //range of values:  2 exp 0 ... (2 exp 16) - 1   BIT-times             
        public ushort LargestStationDelayTime { get; set; }             //largest STATION-DELAY-time:                                          
                                                                        //range of values:  2 exp 0 ... (2 exp 16) - 1   BIT-times             
        public ulong TargetRotationTime { get; set; }                  //TARGET-ROTATION-time:                                                
                                                                       //range of values:  2 exp 0 ... (2 exp 24) - 1   BIT-times             
        public byte GapUpdateFactor { get; set; }                    //GAP-UPDATE-factor: in multiples of ttr                               
                                                                     //range_of_values:  1 ... 100                                          
                                                                     //# ifdef M_DOS
                                                                     //        flc_boolean in_ring_desired { get; set; }       //request entrance into the token-ring                                 
                                                                     //        enum physical_layer  physical_layer { get; set; }        //RS485, modem                                                         
                                                                     //#else
        public short InRingDesired { get; set; }       //request entrance into the token-ring                                 
        public short PhysicalLayer { get; set; }        //RS485, modem                                                         

        public Ident Ident { get; set; }                 //vendor-name, controller_type, version of hardware and software       
    }
}
