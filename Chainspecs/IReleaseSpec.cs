namespace Nethermind.Specs;
public interface IReleaseSpec
{
    public string Name { get; }
    public long MaximumExtraDataSize { get; }
    public long MaxCodeSize { get; }
    public long MinGasLimit { get; }
    public long GasLimitBoundDivisor { get; }
    public long BlockReward { get; }
    public long DifficultyBombDelay { get; }
    public long DifficultyBoundDivisor { get; }
    public long? FixedDifficulty { get; }
    public int MaximumUncleCount { get; }
    public bool IsTimeAdjustmentPostOlympic { get; }
    public bool IsEip2Enabled { get; }
    public bool IsEip7Enabled { get; }
    public bool IsEip100Enabled { get; }
    public bool IsEip140Enabled { get; }
    public bool IsEip150Enabled { get; }
    public bool IsEip155Enabled { get; }
    public bool IsEip158Enabled { get; }
    public bool IsEip160Enabled { get; }
    public bool IsEip170Enabled { get; }
    public bool IsEip196Enabled { get; }
    public bool IsEip197Enabled { get; }
    public bool IsEip198Enabled { get; }
    public bool IsEip211Enabled { get; }
    public bool IsEip214Enabled { get; }
    public bool IsEip649Enabled { get; }
    public bool IsEip658Enabled { get; }
    public bool IsEip145Enabled { get; }
    public bool IsEip1014Enabled { get; }
    public bool IsEip1052Enabled { get; }
    public bool IsEip1283Enabled { get; }
    public bool IsEip1234Enabled { get; }
    public bool IsEip1344Enabled { get; }
    public bool IsEip2028Enabled { get; }
    public bool IsEip152Enabled { get; }
    public bool IsEip1108Enabled { get; }
    public bool IsEip1884Enabled { get; }
    public bool IsEip2200Enabled { get; }
    public bool IsEip2315Enabled { get; }
    public bool IsEip2537Enabled { get; }
    public bool IsEip2565Enabled { get; }
    public bool IsEip2929Enabled { get; }
    public bool IsEip2930Enabled { get; }
    public bool IsEip1559Enabled { get; }
    public bool IsEip3198Enabled { get; }
    public bool IsEip3529Enabled { get; }
    public bool IsEip3607Enabled { get; }
    public bool IsEip3541Enabled { get; }
    public bool IsEip3540Enabled { get; }
    public bool ValidateChainId { get; }
    public bool ValidateReceipts { get; }
    public long? Eip1559BaseFeeMinValue { get; }
    public bool IsEip1153Enabled { get; }
    public bool IsEip3675Enabled { get; }
    public bool IsEip3651Enabled { get; }
    public bool IsEip3670Enabled { get; }
    public bool IsEip3855Enabled { get; }
    public bool IsEip3860Enabled { get; }
    public bool IsEip4200Enabled { get; }
    public bool IsEip4750Enabled { get; }
    public bool IsEip5450Enabled { get; }
    public long Eip1559TransitionBlock { get; }
    public bool ClearEmptyAccountWhenTouched => IsEip158Enabled;
    public bool LimitCodeSize => IsEip170Enabled;
    public bool UseHotAndColdStorage => IsEip2929Enabled;
    public bool UseTxAccessLists => IsEip2930Enabled;
    public bool AddCoinbaseToTxAccessList => IsEip3651Enabled;
    public bool ModExpEnabled => IsEip198Enabled;
    public bool Bn128Enabled => IsEip196Enabled && IsEip197Enabled;
    public bool BlakeEnabled => IsEip152Enabled;
    public bool Bls381Enabled => IsEip2537Enabled;
    public bool ChargeForTopLevelCreate => IsEip2Enabled;
    public bool FailOnOutOfGasCodeDeposit => IsEip2Enabled;
    public bool UseShanghaiDDosProtection => IsEip150Enabled;
    public bool UseExpDDosProtection => IsEip160Enabled;
    public bool UseLargeStateDDosProtection => IsEip1884Enabled;
    public bool ReturnDataOpcodesEnabled => IsEip211Enabled;
    public bool ChainIdOpcodeEnabled => IsEip1344Enabled;
    public bool Create2OpcodeEnabled => IsEip1014Enabled;
    public bool DelegateCallEnabled => IsEip7Enabled;
    public bool StaticCallEnabled => IsEip214Enabled;
    public bool ShiftOpcodesEnabled => IsEip145Enabled;
    public bool SubroutinesEnabled => IsEip2315Enabled;
    public bool StaticRelativeJumpsEnabled => IsEip4200Enabled;
    public bool FunctionSections => IsEip4750Enabled;
    public bool RevertOpcodeEnabled => IsEip140Enabled;
    public bool ExtCodeHashOpcodeEnabled => IsEip1052Enabled;
    public bool SelfBalanceOpcodeEnabled => IsEip1884Enabled;
    public bool UseConstantinopleNetGasMetering => IsEip1283Enabled;
    public bool UseIstanbulNetGasMetering => IsEip2200Enabled;
    public bool UseNetGasMetering => UseConstantinopleNetGasMetering | UseIstanbulNetGasMetering;
    public bool UseNetGasMeteringWithAStipendFix => UseIstanbulNetGasMetering;
    public bool Use63Over64Rule => UseShanghaiDDosProtection;
    public bool BaseFeeEnabled => IsEip3198Enabled;
    public bool IncludePush0Instruction => IsEip3855Enabled;
    public bool TransientStorageEnabled => IsEip1153Enabled;
}