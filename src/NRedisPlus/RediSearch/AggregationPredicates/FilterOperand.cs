namespace NRedisPlus.RediSearch
{
    public class FilterOperand
    {
        private string _text;
        private FilterOperandType _operandType;
        public FilterOperand(string text, FilterOperandType operandType)
        {
            _text = text;
            _operandType = operandType;
        }
        public override string ToString()
        {
            return _operandType switch
            {
                FilterOperandType.Identifier => $"@{_text}",
                FilterOperandType.Numeric => _text,
                FilterOperandType.String => $"'{_text}'",
                _ => ""
            };
        }
    }
}
