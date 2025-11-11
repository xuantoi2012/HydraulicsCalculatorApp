namespace HydraulicsCalculatorApp.Models
{
    public enum InletType
    {
        Grate,              // Hố ga có rể chắn rác
        Curb,               // Hố ga kiểu mép đá
        Combination,        // Hố ga kết hợp (grate + curb)
        Slotted            // Hố ga có khe
    }

    public enum GrateType
    {
        Parallel,          // Rể song song với dòng chảy
        Perpendicular,     // Rể vuông góc với dòng chảy
        P_50_50           // Rể 45-45 (P-50 x 50)
    }

    public enum CurbOpeningType
    {
        Horizontal,        // Lỗ nằm ngang
        Vertical,          // Lỗ thẳng đứng
        Depressed         // Lỗ có đào sâu
    }
}