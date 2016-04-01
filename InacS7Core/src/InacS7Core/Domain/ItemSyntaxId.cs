namespace InacS7Core.Domain
{
    public enum ItemSyntaxId : byte
    {
        S7Any = 0x10,       /* Address data S7-Any pointer-like DB1.DBX10.2 */
        DriveEsAny = 0xa2,  /* seen on Drive ES Starter with routing over S7 */
        Sym1200 = 0xb2,     /* Symbolic address mode of S7-1200 */
        DbRead = 0xb0,      /* Kind of DB block read, seen only at an S7-400 */
        Nck = 0x82,         /* Sinumerik NCK HMI access */
    }
}
