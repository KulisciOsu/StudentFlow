namespace StudentFlow.Core.Authentication;

/// <summary>Jediný typ účtu uživatele; vedení konkrétního projektu se určuje samostatně.</summary>
public enum AccountType
{
    Member,
    Teacher
}

/// <summary>Převádí doménové typy na společné kódy API a databáze; člen používá kód STUDENT.</summary>
public static class AccountTypeCodes
{
    /// <summary>Vrátí kód podporovaného typu účtu; neznámou hodnotu enumu odmítne.</summary>
    public static string ToCode(AccountType accountType) => accountType switch
    {
        AccountType.Member => "STUDENT",
        AccountType.Teacher => "TEACHER",
        _ => throw new ArgumentOutOfRangeException(nameof(accountType))
    };

    /// <summary>Převede uložený kód; neznámý kód považuje za nekonzistentní data.</summary>
    public static AccountType FromCode(string code) => code switch
    {
        "STUDENT" => AccountType.Member,
        "TEACHER" => AccountType.Teacher,
        _ => throw new InvalidOperationException("The stored account type is invalid.")
    };
}
