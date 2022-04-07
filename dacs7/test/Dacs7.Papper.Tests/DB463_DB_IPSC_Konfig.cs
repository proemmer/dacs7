

using Papper.Attributes;

namespace Insite.Customer.Data.DB_IPSC_Konfig
{



    public class UDT_IPSC_DatKonfig_D
    {
        public bool VSTDaten { get; set; }	//Vorstopper Daten der angegebenen BST Nummer holen
        public bool AlleDaten { get; set; }	//Alle Daten holen
        public bool Fahrkurs { get; set; }	//Nur Fahrkurs-Daten holen
        public bool BtIntern { get; set; }	//Nur Band Interne Daten holen
        public bool Motordaten { get; set; }	//Nur Motordaten holen
        public bool Sachnummer { get; set; }	//Nur Sachnummer-Daten holen
        public bool MaschProg { get; set; }	//Nur MaschinenProgramme holen
        public bool Pusy { get; set; }	//Nur Pusy-Daten holen
        public bool Arbeitszeiten { get; set; }	//Nur ArbeitszeitDaten holen
        public bool StandByTexte { get; set; }	//Nur StandByTexte holen
        public bool RepaDaten { get; set; }	//Nur Reparatur Daten holen
        public bool schreiben_auf_DataTag { get; set; }	//Die angeforderten Daten werden am Stopper auf den Datenträger geschrieben
        public short BtInternNo { get; set; }	//Nummer des rauszuholenden Bauteiles, bei 0 = gesamter BT_Int Bereich
    }



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
        public short Aktion { get; set; }
        public short Anzahl { get; set; }
        public short Signal_Auslauf { get; set; }
    }



    public class DB_IPSC_Konfig_Daten
    {
        [ArrayBounds(1, 6, 0)]
        public UDT_IPSC_DatKonfig[] BST { get; set; }
    }



    public class DB_IPSC_Konfig_ZP_Daten
    {
        public bool InvertierteAuswertung { get; set; }	//Wenn 1 dann wir ddie Auswertung invertiert (egal ob MOAR / od Sachnummer)
        public bool vonVST { get; set; }	//Daten sollen vom Vorstopper gesendet werden
        public bool MONR { get; set; }	//Motornummer mitsenden
        public bool APOI { get; set; }	//APO-ID mitsenden
        public bool RPZE { get; set; }	//Repa Zeit mitsenden
        public bool PLAB { get; set; }	//Package Label mitsenden
        public bool KOMP { get; set; }	//Verbaute Komponente
        public bool LPNR { get; set; }	//LieferplanNummer mitsenden

        [ArrayBounds(1, 4, 0)]
        public char[] KOMP_ID { get; set; }	//Id der verbauten Komponente (z.B. ZKCD) mitsenden

        [ArrayBounds(1, 4, 0)]
        public char[] ID { get; set; }  //Wenn keine Motornummer, dann ID für Komponente (z.B. ZKCD) mitsenden

    }



    public class DB_IPSC_Konfig_ZP_ZielAusw
    {
        public bool Aktiv { get; set; }
        public bool vorhanden { get; set; }
        public short von { get; set; }
        public short bis { get; set; }

    }



    public class DB_IPSC_Konfig_ZP_Ausw
    {
        public UDT_DatenAusw_Univ UNIV_Auswertung { get; set; }

        public UDT_DatenErgeb_Univ UNIV_Ergebnis { get; set; }

    }



    public class DB_IPSC_Konfig_ZP_RFDaten
    {
        public bool Aktiv { get; set; }
        public bool vorhanden { get; set; }

        public short BtIntNr { get; set; }

        [StringLength(4)]
        public string Kbez { get; set; }    //Kurzbezeichnung

        [StringLength(14)]
        public string Daten { get; set; }   //Daten bis zu 14 Zeichen

    }



    public class DB_IPSC_Konfig_ZP
    {

        [StringLength(40)]
        public string InfoText { get; set; }    //Info für Winccflex

        [StringLength(18)]
        public string ZP_Name { get; set; }	//Zählpunktname z:b: T03_1LM9
        public bool UNIV_aktiv { get; set; }	//Universal Auswertung aktiv
        public bool nochmal_versenden { get; set; }	//Zählpunkt nochmal versenden
        public DB_IPSC_Konfig_ZP_Daten Daten { get; set; }
        public DB_IPSC_Konfig_ZP_ZielAusw ZielAusw { get; set; }	//Zielauswertung von - bis Jis wird auch bei nicht Wt_mit_Bearb gesendet
        public DB_IPSC_Konfig_ZP_Ausw Ausw { get; set; }
        public DB_IPSC_Konfig_ZP_RFDaten RFDaten { get; set; }  //Daten die in den BtInternen Bereich geschrieben werden sollen
    }



    public class DB_IPSC_Konfig_StatusAnzeige_ZP
    {
        public bool Fertig { get; set; }
        public bool Error { get; set; }

        [StringLength(40)]
        public string ErrText { get; set; }
    }



    public class DB_IPSC_Konfig_ZP_HMI_manuell
    {
        public bool Aktiv { get; set; }
        public bool Start { get; set; }
        public UDT_IPSC_ZP_DAT Data { get; set; }
    }



    public class UDT_IPSC_DatKonfig
    {
        public UDT_IPSC_DatKonfig_D D { get; set; }
    }


    public class UDT_DatenAusw_Univ
    {

        [ArrayBounds(1, 24, 0)]
        public UDT_DatenAusw_Univ_Ausw[] Ausw { get; set; } //24 Auswertungen
    }


    public class UDT_DatenErgeb_Univ
    {
        [ReadOnly(true)]
        public short IO_Nr { get; set; }    //Nr der gefundenen Auswertung

        [ReadOnly(true)]
        public short Fehler_Nr { get; set; }    //Fehler Nummer

        [ReadOnly(true)]
        public short Fehler_Position { get; set; }  //Zeilen Position des Fehlers

        [ReadOnly(true)]
        [StringLength(80)]
        public string Fehler_Text { get; set; } //Fehler Text
    }


    public class UDT_IPSC_ZP_DAT
    {

        [ArrayBounds(1, 18, 0)]
        public char[] ZP_Name { get; set; }	//z.B. T03_1LM9

        [ArrayBounds(1, 4, 0)]
        public char[] Id_Name { get; set; }	//z.B. MONR

        [ArrayBounds(1, 40, 0)]
        public char[] Id_Daten { get; set; }	//z.B. 12345678

        [ArrayBounds(1, 4, 0)]
        public char[] APOID { get; set; }	//fix APOID wenn diese mitgesendet werden soll

        [ArrayBounds(1, 12, 0)]
        public char[] APOID_Daten { get; set; }	//z.B. 12345678

        [ArrayBounds(1, 4, 0)]
        public char[] Daten_Kurzbezeichnung { get; set; }	//Anbauteil z.B. ZYLK

        [ArrayBounds(1, 40, 0)]
        public char[] Daten { get; set; }	//z.B. 12345678

        [ArrayBounds(1, 2, 0)]
        public char[] Daten_Anzahl { get; set; }	//Anzahl Teile

        [ArrayBounds(1, 4, 0)]
        public char[] Repazeit_Name { get; set; }	//z.B. RPZE

        [ArrayBounds(1, 6, 0)]
        public char[] Repazeit_Daten { get; set; }	//z.B. 000240

        [ArrayBounds(1, 4, 0)]
        public char[] Packagelabel_Name { get; set; }	//z.B. PLAB

        [ArrayBounds(1, 20, 0)]
        public char[] Packagelabel_Daten { get; set; }

        [ArrayBounds(1, 4, 0)]
        public char[] Lieferplannummer_Name { get; set; }	//z.B. LPNR

        [ArrayBounds(1, 20, 0)]
        public char[] Lieferplannummer_Daten { get; set; }

    }

    [Mapping("DB_IPSC_Konfig", "DB463", 0)]
    public class DB_IPSC_Konfig
    {
        public DB_IPSC_Konfig_Daten Daten { get; set; }

        [StringLength(14)]
        public string Reserve { get; set; }

        [ArrayBounds(1, 10, 0)]
        public DB_IPSC_Konfig_ZP[] ZP { get; set; }

        [StringLength(48)]
        public string Reserve1 { get; set; }

        [ArrayBounds(1, 10, 0)]
        public DB_IPSC_Konfig_StatusAnzeige_ZP[] StatusAnzeige_ZP { get; set; }

        [StringLength(88)]
        public string Reserve2 { get; set; }
        public DB_IPSC_Konfig_ZP_HMI_manuell ZP_HMI_manuell { get; set; }

    }

}

