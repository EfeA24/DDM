using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Filtering
{
    public abstract record FilterNode;

    public sealed record BinaryFilterNode(string Operator, FilterNode Left, FilterNode Right) : FilterNode;

    public sealed record UnaryFilterNode(string Operator, FilterNode Operand) : FilterNode;

    public sealed record FunctionFilterNode(string Name, IReadOnlyList<FilterNode> Arguments) : FilterNode;

    public sealed record PropertyFilterNode(string Name) : FilterNode;

    public sealed record LiteralFilterNode(object? Value) : FilterNode;
}
