namespace StudentFlow.Core.Authentication;

public enum AccountType
{
    Member,
    Teacher
}

public static class AccountTypeCodes
{
    public static string ToCode(AccountType accountType) => accountType switch
    {
        AccountType.Member => "STUDENT",
        AccountType.Teacher => "TEACHER",
        _ => throw new ArgumentOutOfRangeException(nameof(accountType))
    };

    public static AccountType FromCode(string code) => code switch
    {
        "STUDENT" => AccountType.Member,
        "TEACHER" => AccountType.Teacher,
        _ => throw new InvalidOperationException("The stored account type is invalid.")
    };
}
