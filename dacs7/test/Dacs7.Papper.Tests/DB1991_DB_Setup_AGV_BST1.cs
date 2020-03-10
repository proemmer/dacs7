using Papper.Attributes;
using System;


namespace Customer.Data.DB_Setup_AGV_BST1
{
    
    

    public class UDT_DatenAusw_Univ_Ausw
    {
        
        
        [StringLength(6)]
        public string Bez1 { get; set; }
        
        [StringLength(9)]
        public string Data1 { get; set; }
        
        [StringLength(6)]
        public string Bez2 { get; set; }
        
        [StringLength(9)]
        public string Data2 { get; set; }
        public Int16 Aktion { get; set; }
        public Int16 Anzahl { get; set; }
        public Int16 Signal_Auslauf { get; set; }
    }

    

    public class UDT_Check_Limits_A_Axis
    {
        [ReadOnly(true)]
        public bool Err_Lim_Pos { get; set; }	//Error positive limit
        [ReadOnly(true)]
        public bool Err_Pos { get; set; }	//Error position
        [ReadOnly(true)]
        public bool Err_Lim_Neg { get; set; }	//Error negative limit
    }

    

    public class UDT_Check_Limits_Z_Axis
    {
        [ReadOnly(true)]
        public bool Err_Lim_Pos { get; set; }	//Error positive limit
        [ReadOnly(true)]
        public bool Err_Pos { get; set; }	//Error position
        [ReadOnly(true)]
        public bool Err_Lim_Neg { get; set; }	//Error negative limit
    }

    

    public class UDT_HMI_Stat_Setup_AGV_Service
    {
        [ReadOnly(true)]
        public bool activ { get; set; }
        [ReadOnly(true)]
        public Int16 max_A { get; set; }
        [ReadOnly(true)]
        public Int16 pos_A { get; set; }
        [ReadOnly(true)]
        public Int16 min_A { get; set; }
        [ReadOnly(true)]
        public Int16 pos_C { get; set; }
        [ReadOnly(true)]
        public Int16 max_Z { get; set; }
        [ReadOnly(true)]
        public Int16 pos_Z { get; set; }
        [ReadOnly(true)]
        public Int16 min_Z { get; set; }
        [ReadOnly(true)]
        public bool Reserve_1 { get; set; }
        [ReadOnly(true)]
        public bool Reserve_2 { get; set; }
        [ReadOnly(true)]
        public bool Reserve_3 { get; set; }
    }

    

    public class UDT_Status_Setup_AGV_Target
    {
        [ReadOnly(true)]
        public Int16 Lim_max_A { get; set; }	//Target limit max. A-Axis
        [ReadOnly(true)]
        public Int16 Pos_A { get; set; }	//Target Position A-Axis
        [ReadOnly(true)]
        public Int16 Lim_min_A { get; set; }	//Target limit min. A-Axis
        [ReadOnly(true)]
        public Int16 Pos_C { get; set; }	//Target Position C-Axis
        [ReadOnly(true)]
        public Int16 Lim_max_Z { get; set; }	//Target limit max. Z-Axis
        [ReadOnly(true)]
        public Int16 Pos_Z { get; set; }	//Target Position Z-Axis
        [ReadOnly(true)]
        public Int16 Lim_min_Z { get; set; }	//Target limit min. Z-Axis
        [ReadOnly(true)]
        public bool Reserve_1 { get; set; }	//Reserve
        [ReadOnly(true)]
        public bool Reserve_2 { get; set; }	//Reserve
        [ReadOnly(true)]
        public bool Reserve_3 { get; set; }	//Reserve
    }

    

    public class UDT_Status_Setup_AGV_Actual
    {
        [ReadOnly(true)]
        public Int16 Pos_A { get; set; }	//Actual Position A-Axis
        [ReadOnly(true)]
        public Int16 Pos_C { get; set; }	//Actual Position C-Axis
        [ReadOnly(true)]
        public Int16 Pos_Z { get; set; }	//Actual Position Z-Axis
    }

    

    public class UDT_Status_Setup_AGV_Reserve
    {
        [ReadOnly(true)]
        public bool Res_01 { get; set; }
        [ReadOnly(true)]
        public bool Res_02 { get; set; }
        [ReadOnly(true)]
        public bool Res_03 { get; set; }
        [ReadOnly(true)]
        public bool Res_04 { get; set; }
        [ReadOnly(true)]
        public bool Res_05 { get; set; }
        [ReadOnly(true)]
        public bool Res_06 { get; set; }
        [ReadOnly(true)]
        public bool Res_07 { get; set; }
    }

    

    public class UDT_Status_Setup_AGV_out_FB_Setup_AGV
    {
        [ReadOnly(true)]
        public bool done { get; set; }	//finish evaluation (positions of actual type+step would be evaluated)
        [ReadOnly(true)]
        public bool release_process { get; set; }	//release process from Setup_AGV_FB
        [ReadOnly(true)]
        public bool release_outlet { get; set; }	//release Outlet from Setup_AGV_FB
        [ReadOnly(true)]
        public bool Error { get; set; }	//Error at evaluation
        [ReadOnly(true)]
        public Int16 Error_status { get; set; }	//Error status of FB991 (Error-Description in FB991)
    }

    

    public class UDT_Setup_AGV_Types_Typ_step
    {
        
        [StringLength(14)]
        public string step_name { get; set; }	//Step Name configured on HMI (no evaluation)
        public Int16 limit_pos_A { get; set; }	//Positive limit
        public Int16 pos_A { get; set; }	//Configured position on HMI
        public Int16 limit_neg_A { get; set; }	//negative limit
        public Int16 pos_C { get; set; }	//Configured position on HMI
        public Int16 limit_pos_Z { get; set; }	//Positive limit
        public Int16 pos_Z { get; set; }	//Configured position on HMI
        public Int16 limit_neg_Z { get; set; }	//negative limit
        public bool Reserve_1 { get; set; }	//Reserve
        public bool Reserve_2 { get; set; }	//Reserve
        public bool Reserve_3 { get; set; }	//Reserve
        public bool disable_eval_In { get; set; }	//TRUE = Evaluation of Engine Pos. not required
        public bool disable_eval_Out { get; set; }	//TRUE = Evaluation of Engine Pos. not required
        public bool Ignore_A_Axis { get; set; }	//TRUE = Position will not send to AGV-controller
        public bool Ignore_C_Axis { get; set; }	//TRUE = Position will not send to AGV-controller
        public bool Ignore_Z_Axis { get; set; }	//TRUE = Position will not send to AGV-controller
    }

    

    public class UDT_Setup_AGV_Types_Typ
    {
        [ReadOnly(true)]
		public bool active { get; set; }	//Type activated on HMI
        
        [StringLength(8)]
        public string type_name { get; set; }	//Type name configured on HMI (no evaluation)

        [ArrayBounds(1,10,0)]
        public UDT_Setup_AGV_Types_Typ_step[] step { get; set; }
    }

    

    public class UDT_Lock_Unlock_Axis_PrePos
    {
        [ReadOnly(true)]
        public bool Lock_A_Axis { get; set; }	//Lock A-Axis
        [ReadOnly(true)]
        public bool Unlock_A_Axis { get; set; }	//Unock A-Axis
        [ReadOnly(true)]
        public bool Lock_Z_Axis { get; set; }	//Lock Z-Axis
        [ReadOnly(true)]
        public bool Unlock_Z_Axis { get; set; }	//Unock Z-Axis
    }

    

    public class UDT_Lock_Unlock_Axis_StopPos
    {
        [ReadOnly(true)]
        public bool Lock_A_Axis { get; set; }	//Lock A-Axis
        [ReadOnly(true)]
        public bool Unlock_A_Axis { get; set; }	//Unock A-Axis
        [ReadOnly(true)]
        public bool Lock_Z_Axis { get; set; }	//Lock Z-Axis
        [ReadOnly(true)]
        public bool Unlock_Z_Axis { get; set; }	//Unock Z-Axis
    }


    
    public class UDT_DatenErgeb_Univ
    {
        [ReadOnly(true)]
        public Int16 IO_Nr { get; set; }	//Nr der gefundenen Auswertung
        [ReadOnly(true)]
        public Int16 Fehler_Nr { get; set; }	//Fehler Nummer
        [ReadOnly(true)]
        public Int16 Fehler_Position { get; set; }	//Zeilen Position des Fehlers
        [ReadOnly(true)]
        
        [StringLength(80)]
        public string Fehler_Text { get; set; }	//Fehler Text

    }

    
    public class UDT_DatenAusw_Univ
    {
        

        [ArrayBounds(1,24,0)]
        public UDT_DatenAusw_Univ_Ausw[] Ausw { get; set; }	//24 Auswertungen

    }

    
    public class UDT_Status_PC477
    {
        [ReadOnly(true)]
        public Int16 Pw_Level { get; set; }	//Password level of PC477
        [ReadOnly(true)]
        public Int16 Status_Teach_in { get; set; }	//0=Pw level not sufficient; 1=no activity; 2=in work; 3=successfully; 4=Error;
        [ReadOnly(true)]
        public Int16 Satus_save_para { get; set; }	//Status save parameter, only for HMI
        [ReadOnly(true)]
        public Int16 Step_Status { get; set; }	//Status of Target Step
        [ReadOnly(true)]
        public Int16 Reserve_1 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_2 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_3 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_4 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_5 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_6 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_7 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_8 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_9 { get; set; }
        [ReadOnly(true)]
        public Int16 Reserve_10 { get; set; }
        [ReadOnly(true)]
        public bool Pos_invisible { get; set; }	//Positions on HMI invisible if no AGV available
        [ReadOnly(true)]
        public bool Res_Bool_1 { get; set; }
        [ReadOnly(true)]
        public bool Res_Bool_2 { get; set; }
        [ReadOnly(true)]
        public bool Res_Bool_3 { get; set; }
        [ReadOnly(true)]
        public bool Res_Bool_4 { get; set; }
        [ReadOnly(true)]
        public bool Res_Bool_5 { get; set; }
        [ReadOnly(true)]
        public bool Res_Bool_6 { get; set; }
        [ReadOnly(true)]
        public bool Res_Bool_7 { get; set; }

    }

    
    public class UDT_Check_Limits
    {
        [ReadOnly(true)]
        public UDT_Check_Limits_A_Axis A_Axis { get; set; }
        [ReadOnly(true)]
        public UDT_Check_Limits_Z_Axis Z_Axis { get; set; }
        [ReadOnly(true)]
        public bool C_Axis_no_Pos { get; set; }	//No Position configured

    }

    
    public class UDT_HMI_Stat_Setup_AGV
    {
        [ReadOnly(true)]
        public Int16 Step_Status { get; set; }
        [ReadOnly(true)]
        public UDT_HMI_Stat_Setup_AGV_Service Service { get; set; }
        [ReadOnly(true)]
        public bool Show_Setp_Stat_PrePos { get; set; }
        [ReadOnly(true)]
        public Int16 Step_Status_PrePos { get; set; }
        [ReadOnly(true)]
        public bool Service_Err_max_A { get; set; }	//Service Screen Error Limit max A-Axis
        [ReadOnly(true)]
        public bool Service_Err_pos_A { get; set; }	//Service Screen Error Pos A-Axis
        [ReadOnly(true)]
        public bool Service_Err_min_A { get; set; }	//Service Screen Error Limit min A-Axis
        [ReadOnly(true)]
        public bool Service_Err_max_Z { get; set; }	//Service Screen Error Limit max Z-Axis
        [ReadOnly(true)]
        public bool Service_Err_pos_Z { get; set; }	//Service Screen Error Pos Z-Axis
        [ReadOnly(true)]
        public bool Service_Err_min_Z { get; set; }	//Service Screen Error Limit min Z-Axis
        [ReadOnly(true)]
        public bool Service_Res_06 { get; set; }
        [ReadOnly(true)]
        public bool Service_Res_07 { get; set; }
        [ReadOnly(true)]

        [ArrayBounds(1,14,0)]
        public byte[] Reserve_01 { get; set; }
        [ReadOnly(true)]

        [ArrayBounds(1,14,0)]
        public byte[] Reserve_02 { get; set; }
        [ReadOnly(true)]
        public bool Teach_in { get; set; }	//Teach in actual Position of AGV
        [ReadOnly(true)]
        public Int16 Status_Teach_in { get; set; }	//0=Pw level not sufficient; 1=no activity; 2=in work; 3=successfully; 4=Error;
        [ReadOnly(true)]
        public Int16 sel_Type { get; set; }	//Actual selected Type on HMI
        [ReadOnly(true)]
        public Int16 sel_Step { get; set; }	//Actual selected Step on HMI
        [ReadOnly(true)]
        public Int16 count_steps_of_sel_type { get; set; }	//Count steps of actual selected type
        [ReadOnly(true)]
        public bool refresh_screen { get; set; }	//Refresh Setup screen (only PC477)
        [ReadOnly(true)]
        public bool overview_activ { get; set; }	//Overview activ
        [ReadOnly(true)]
        public bool save_config { get; set; }	//save configuration to parasave/plc data manager
        [ReadOnly(true)]
        public Int16 Satus_save_para { get; set; }	//Status save parameter, only for HMI
        [ReadOnly(true)]
        public bool finish_eval { get; set; }	//Finish evaluation of selected type
        [ReadOnly(true)]
        public bool Send_data_manual { get; set; }	//Send target setup data manual -- triggert on HMI
        [ReadOnly(true)]
        public bool Conf_typenames_activ { get; set; }	//Configuration of type names active
        [ReadOnly(true)]
        public bool Conf_stepnames_activ { get; set; }	//Configuration of step names active

    }

    
    public class UDT_Status_Setup_AGV
    {
        [ReadOnly(true)]
        public UDT_Status_Setup_AGV_Target Target { get; set; }
        [ReadOnly(true)]
        public UDT_Status_Setup_AGV_Actual Actual { get; set; }
        [ReadOnly(true)]
        public UDT_Status_Setup_AGV_Reserve Reserve { get; set; }
        [ReadOnly(true)]
        public UDT_Status_Setup_AGV_out_FB_Setup_AGV out_FB_Setup_AGV { get; set; }

    }

    
    public class UDT_Setup_AGV_Types
    {
        [ReadOnly(true)]  
        public bool Regist_AGV { get; set; }	//Registrade AGV in Station
        [ReadOnly(true)]
		public Int16 act_Type { get; set; }	//Actual Type in Station 1..24
        [ReadOnly(true)]
		public Int16 target_step { get; set; }	//target assembly/process step/position
        [ReadOnly(true)]
		public bool target_step_ok { get; set; }	//target step reached
       
		public bool eval_PrePos { get; set; }	//Evaluation at Pre-Position enabled
        [ReadOnly(true)]
		public bool Pos_invisible { get; set; }	//Position on HMI invisible (invalid Pos. or no Type in Station)
        
		public bool Without_UNIV_Eval { get; set; }	//No UNIV evaluation, Operator musn´t set an Engine Position
        [ReadOnly(true)]
		public bool Reset { get; set; }	//Reset Step to beginning
        [ReadOnly(true)]
		public bool Reenter_AGV { get; set; }	//Reenter AGV, override Input Signals from AGV to reset data
        [ReadOnly(true)]
		public bool Rel_Send_manual { get; set; }	//Release to send Engine Pos. manually
        [ReadOnly(true)]
		public bool PrePos_AGV_avail { get; set; }	//AGV available at PrePos
        [ReadOnly(true)]
		public bool HMI_Active { get; set; }	//HMI (only PC477) is activated (Operating Coordination)
        [ReadOnly(true)]
		public bool Res_01 { get; set; }
        [ReadOnly(true)]
  	    public bool Res_02 { get; set; }
        [ReadOnly(true)]
		public bool Res_03 { get; set; }
        [ReadOnly(true)]
		public bool Res_04 { get; set; }
        [ReadOnly(true)]
		public bool Res_05 { get; set; }
        [ReadOnly(true)]
		public bool Res_06 { get; set; }
        [ReadOnly(true)]
		public bool Res_07 { get; set; }

        [ArrayBounds(1,24,0)]
        public UDT_Setup_AGV_Types_Typ[] Typ { get; set; }

    }

    
    public class UDT_Lock_Unlock_Axis
    {
        [ReadOnly(true)]
        public UDT_Lock_Unlock_Axis_PrePos PrePos { get; set; }
        [ReadOnly(true)]
        public UDT_Lock_Unlock_Axis_StopPos StopPos { get; set; }

    }

    [Mapping("DB_Setup_AGV_BST1", "DB1991", 0)]
    public class DB_Setup_AGV_BST1
    {
        public UDT_Setup_AGV_Types Setup { get; set; }
        public UDT_DatenAusw_Univ UNIV { get; set; }
        [ReadOnly(true)]
        public UDT_DatenErgeb_Univ UNIV_Result_PrePos { get; set; }
        [ReadOnly(true)]
        public UDT_DatenErgeb_Univ UNIV_Result_StopPos { get; set; }
        [ReadOnly(true)]
        public UDT_Status_Setup_AGV PrePos { get; set; }
        [ReadOnly(true)]
        public UDT_Status_Setup_AGV StopPos { get; set; }
        [ReadOnly(true)]
        public UDT_HMI_Stat_Setup_AGV Service { get; set; }
        [ReadOnly(true)]
        public UDT_Check_Limits Check_Lim { get; set; }
        [ReadOnly(true)]
        public bool q_Error_Limits { get; set; }
        [ReadOnly(true)]

        [ArrayBounds(1,10,0)]
        public bool[] Step_Engine_Pos_ok { get; set; }
        [ReadOnly(true)]
        public UDT_Status_PC477 Status_PC477 { get; set; }
        [ReadOnly(true)]
        public UDT_Lock_Unlock_Axis Lock_Unlock { get; set; }

    }

}

