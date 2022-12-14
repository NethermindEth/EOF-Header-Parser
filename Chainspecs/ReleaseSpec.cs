namespace Nethermind.Specs;
public class ReleaseSpec : IReleaseSpec
{
    public string Name { get; set; } = "Custom";
    public long MaximumExtraDataSize { get; set; }
    public long MaxCodeSize { get; set; }
    public long MinGasLimit { get; set; }
    public long GasLimitBoundDivisor { get; set; }
    public long BlockReward { get; set; }
    public long DifficultyBombDelay { get; set; }
    public long DifficultyBoundDivisor { get; set; }
    public long? FixedDifficulty { get; set; }
    public int MaximumUncleCount { get; set; }
    public bool IsTimeAdjustmentPostOlympic { get; set; }
    public bool IsEip2Enabled { get; set; }
    public bool IsEip7Enabled { get; set; }
    public bool IsEip100Enabled { get; set; }
    public bool IsEip140Enabled { get; set; }
    public bool IsEip150Enabled { get; set; }
    public bool IsEip155Enabled { get; set; }
    public bool IsEip158Enabled { get; set; }
    public bool IsEip160Enabled { get; set; }
    public bool IsEip170Enabled { get; set; }
    public bool IsEip196Enabled { get; set; }
    public bool IsEip197Enabled { get; set; }
    public bool IsEip198Enabled { get; set; }
    public bool IsEip211Enabled { get; set; }
    public bool IsEip214Enabled { get; set; }
    public bool IsEip649Enabled { get; set; }
    public bool IsEip658Enabled { get; set; }
    public bool IsEip145Enabled { get; set; }
    public bool IsEip1014Enabled { get; set; }
    public bool IsEip1052Enabled { get; set; }
    public bool IsEip1283Enabled { get; set; }
    public bool IsEip1234Enabled { get; set; }
    public bool IsEip1344Enabled { get; set; }
    public bool IsEip2028Enabled { get; set; }
    public bool IsEip152Enabled { get; set; }
    public bool IsEip1108Enabled { get; set; }
    public bool IsEip1884Enabled { get; set; }
    public bool IsEip2200Enabled { get; set; }
    public bool IsEip2315Enabled { get; set; }
    public bool IsEip2537Enabled { get; set; }
    public bool IsEip2565Enabled { get; set; }
    public bool IsEip2929Enabled { get; set; }
    public bool IsEip2930Enabled { get; set; }
    public bool IsEip1559Enabled { get; set; }
    public bool IsEip3198Enabled { get; set; }
    public bool IsEip3529Enabled { get; set; }
    public bool IsEip3607Enabled { get; set; }
    public bool IsEip3541Enabled { get; set; }
    public bool IsEip3540Enabled { get; set; }
    public bool ValidateChainId { get; set; }
    public bool ValidateReceipts { get; set; }
    public long Eip1559TransitionBlock { get; set; }
    public long? Eip1559BaseFeeMinValue { get; set; }
    public bool IsEip1153Enabled { get; set; }
    public bool IsEip3675Enabled { get; set; }
    public bool IsEip3651Enabled { get; set; }
    public bool IsEip3670Enabled { get; set; }
    public bool IsEip3855Enabled { get; set; }
    public bool IsEip3860Enabled { get; set; }
    public bool IsEip4200Enabled { get; set; }
    public bool IsEip4750Enabled { get; set; }
    public bool IsEip5450Enabled { get; set; }
    
}