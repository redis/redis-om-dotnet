namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// A representation of a filter operand.
    /// </summary>
    internal class FilterOperand
    {
        private readonly string _text;
        private readonly FilterOperandType _operandType;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterOperand"/> class.
        /// </summary>
        /// <param name="text">the text.</param>
        /// <param name="operandType">the operand type.</param>
        internal FilterOperand(string text, FilterOperandType operandType)
        {
            _text = text;
            _operandType = operandType;
        }

        /// <summary>
        /// Sends the operand to a string.
        /// </summary>
        /// <returns>String representation of the operand.</returns>
        public override string ToString()
        {
            return _operandType switch
            {
                FilterOperandType.Identifier => $"@{_text}",
                FilterOperandType.Numeric => _text,
                FilterOperandType.String => $"'{_text}'",
                _ => string.Empty
            };
        }
    }
}
