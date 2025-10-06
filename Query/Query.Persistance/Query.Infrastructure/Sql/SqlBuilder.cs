using Dapper;
using Query.Application.Filtering;
using Query.Application.Interfaces;
using Query.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Query.Infrastructure.Sql
{
    public sealed class SqlBuilder
    {
        private static readonly Regex IdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private readonly ISelectAuthorizer _selectAuthorizer;

        public SqlBuilder(ISelectAuthorizer selectAuthorizer)
            => _selectAuthorizer = selectAuthorizer ?? throw new ArgumentNullException(nameof(selectAuthorizer));

        public SqlStatement BuildSelect(ODataSpec spec, Dialect dialect)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var parameters = new DynamicParameters();
            var builder = new StringBuilder();
            int parameterIndex = 0;

            builder.Append("SELECT ");
            if (dialect == Dialect.SqlServer && spec.Top.HasValue && spec.Skip is null)
            {
                var topParam = NextParameterName(parameterIndex++);
                parameters.Add(topParam, spec.Top.Value);
                builder.Append($"TOP (@{topParam}) ");
            }

            AppendSelectList(builder, spec, dialect);
            builder.Append(" FROM ");
            builder.Append(QuoteQualifiedIdentifier(spec.Source, dialect));

            var whereClause = BuildWhereClause(spec.Filter, dialect, parameters, ref parameterIndex);
            if (!string.IsNullOrEmpty(whereClause))
            {
                builder.Append(" WHERE ");
                builder.Append(whereClause);
            }

            var orderClause = BuildOrderByClause(spec, dialect);
            if (!string.IsNullOrEmpty(orderClause))
            {
                builder.Append(' ');
                builder.Append(orderClause);
            }

            AppendPagination(builder, spec, dialect, parameters, ref parameterIndex, string.IsNullOrEmpty(orderClause));

            return new SqlStatement(builder.ToString(), parameters);
        }

        public SqlStatement BuildCount(ODataSpec spec, Dialect dialect)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var parameters = new DynamicParameters();
            int parameterIndex = 0;
            var builder = new StringBuilder();
            builder.Append("SELECT COUNT(*) FROM ");
            builder.Append(QuoteQualifiedIdentifier(spec.Source, dialect));

            var whereClause = BuildWhereClause(spec.Filter, dialect, parameters, ref parameterIndex);
            if (!string.IsNullOrEmpty(whereClause))
            {
                builder.Append(" WHERE ");
                builder.Append(whereClause);
            }

            return new SqlStatement(builder.ToString(), parameters);
        }

        private void AppendSelectList(StringBuilder builder, ODataSpec spec, Dialect dialect)
        {
            if (spec.Select.Count == 0)
            {
                builder.Append('*');
                return;
            }

            var projected = new List<string>(spec.Select.Count);
            foreach (var column in spec.Select)
            {
                if (!_selectAuthorizer.IsAllowed(column))
                {
                    throw new InvalidOperationException($"Column '{column}' is not allowed for selection.");
                }

                projected.Add(QuoteQualifiedIdentifier(column, dialect));
            }

            builder.Append(string.Join(", ", projected));
        }

        private static string? BuildWhereClause(FilterNode? filter, Dialect dialect, DynamicParameters parameters, ref int parameterIndex)
        {
            if (filter is null)
            {
                return null;
            }

            var generator = new FilterSqlGenerator(dialect, parameters, parameterIndex);
            var sql = generator.Generate(filter);
            parameterIndex = generator.ParameterIndex;
            return sql;
        }

        private static string? BuildOrderByClause(ODataSpec spec, Dialect dialect)
        {
            if (spec.OrderBy.Count == 0)
            {
                return null;
            }

            var parts = new List<string>(spec.OrderBy.Count);
            foreach (var clause in spec.OrderBy)
            {
                var identifier = QuoteQualifiedIdentifier(clause.Field, dialect);
                parts.Add($"{identifier} {(clause.Descending ? "DESC" : "ASC")}");
            }

            return $"ORDER BY {string.Join(", ", parts)}";
        }

        private static void AppendPagination(StringBuilder builder, ODataSpec spec, Dialect dialect, DynamicParameters parameters, ref int parameterIndex, bool orderMissing)
        {
            if (spec.Skip is null && spec.Top is null)
            {
                return;
            }

            switch (dialect)
            {
                case Dialect.Postgres:
                    {
                        if (spec.Top.HasValue)
                        {
                            var topParam = NextParameterName(parameterIndex++);
                            parameters.Add(topParam, spec.Top.Value);
                            builder.Append($" LIMIT @{topParam}");
                        }

                        if (spec.Skip.HasValue)
                        {
                            var skipParam = NextParameterName(parameterIndex++);
                            parameters.Add(skipParam, spec.Skip.Value);
                            builder.Append($" OFFSET @{skipParam}");
                        }

                        break;
                    }
                case Dialect.SqlServer:
                    {
                        if (orderMissing)
                        {
                            builder.Append(" ORDER BY (SELECT 1)");
                        }

                        var skipParam = NextParameterName(parameterIndex++);
                        var skipValue = spec.Skip ?? 0;
                        parameters.Add(skipParam, skipValue);
                        builder.Append($" OFFSET @{skipParam} ROWS");

                        if (spec.Top.HasValue)
                        {
                            var topParam = NextParameterName(parameterIndex++);
                            parameters.Add(topParam, spec.Top.Value);
                            builder.Append($" FETCH NEXT @{topParam} ROWS ONLY");
                        }

                        break;
                    }
                case Dialect.Oracle:
                    {
                        if (spec.Skip.HasValue)
                        {
                            var skipParam = NextParameterName(parameterIndex++);
                            parameters.Add(skipParam, spec.Skip.Value);
                            builder.Append($" OFFSET @{skipParam} ROWS");
                        }

                        if (spec.Top.HasValue)
                        {
                            var topParam = NextParameterName(parameterIndex++);
                            parameters.Add(topParam, spec.Top.Value);
                            builder.Append($" FETCH NEXT @{topParam} ROWS ONLY");
                        }

                        break;
                    }
            }
        }

        private static string QuoteQualifiedIdentifier(string identifier, Dialect dialect)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new InvalidOperationException("Identifier cannot be empty.");
            }

            var parts = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                throw new InvalidOperationException("Identifier is invalid.");
            }

            var quoted = parts.Select(part => QuoteIdentifier(part, dialect));
            return string.Join('.', quoted);
        }

        private static string QuoteIdentifier(string identifier, Dialect dialect)
        {
            if (!IdentifierRegex.IsMatch(identifier))
            {
                throw new InvalidOperationException($"Identifier '{identifier}' is invalid.");
            }

            return dialect switch
            {
                Dialect.SqlServer => $"[{identifier}]",
                _ => $"\"{identifier}\""
            };
        }

        private static string NextParameterName(int index) => $"p{index}";

        private sealed class FilterSqlGenerator
        {
            private readonly Dialect _dialect;
            private readonly DynamicParameters _parameters;
            private int _parameterIndex;

            public FilterSqlGenerator(Dialect dialect, DynamicParameters parameters, int parameterIndex)
            {
                _dialect = dialect;
                _parameters = parameters;
                _parameterIndex = parameterIndex;
            }

            public int ParameterIndex => _parameterIndex;

            public string Generate(FilterNode node) => Visit(node);

            private string Visit(FilterNode node) => node switch
            {
                BinaryFilterNode binary => VisitBinary(binary),
                UnaryFilterNode unary => VisitUnary(unary),
                FunctionFilterNode function => VisitFunction(function),
                PropertyFilterNode property => QuoteQualifiedIdentifier(property.Name, _dialect),
                LiteralFilterNode literal => CreateParameter(literal.Value),
                _ => throw new InvalidOperationException("Unsupported filter node type.")
            };

            private string VisitBinary(BinaryFilterNode node)
            {
                var op = node.Operator.ToLowerInvariant();
                if (op is "and" or "or")
                {
                    var left = Visit(node.Left);
                    var right = Visit(node.Right);
                    return $"({left} {op.ToUpperInvariant()} {right})";
                }

                if (op is "eq" or "ne")
                {
                    if (IsNullLiteral(node.Left))
                    {
                        var operand = Visit(node.Right);
                        return op == "eq" ? $"({operand} IS NULL)" : $"({operand} IS NOT NULL)";
                    }

                    if (IsNullLiteral(node.Right))
                    {
                        var operand = Visit(node.Left);
                        return op == "eq" ? $"({operand} IS NULL)" : $"({operand} IS NOT NULL)";
                    }
                }

                var leftExpr = Visit(node.Left);
                var rightExpr = Visit(node.Right);
                var translated = op switch
                {
                    "eq" => "=",
                    "ne" => "<>",
                    "gt" => ">",
                    "ge" => ">=",
                    "lt" => "<",
                    "le" => "<=",
                    _ => throw new InvalidOperationException($"Unsupported operator '{node.Operator}'.")
                };

                return $"({leftExpr} {translated} {rightExpr})";
            }

            private string VisitUnary(UnaryFilterNode node)
            {
                if (!string.Equals(node.Operator, "not", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Unsupported unary operator '{node.Operator}'.");
                }

                return $"(NOT {Visit(node.Operand)})";
            }

            private string VisitFunction(FunctionFilterNode node) => node.Name switch
            {
                "startswith" => BuildLikeExpression(node, suffix: "%"),
                "contains" => BuildLikeExpression(node, prefix: "%", suffix: "%"),
                _ => throw new InvalidOperationException($"Unsupported function '{node.Name}'.")
            };

            private string BuildLikeExpression(FunctionFilterNode node, string? prefix = null, string? suffix = null)
            {
                if (node.Arguments.Count < 2)
                {
                    throw new InvalidOperationException($"Function '{node.Name}' expects two arguments.");
                }

                if (node.Arguments[0] is not PropertyFilterNode property)
                {
                    throw new InvalidOperationException($"The first argument of '{node.Name}' must be a property.");
                }

                if (node.Arguments[1] is not LiteralFilterNode literal || literal.Value is null)
                {
                    throw new InvalidOperationException($"The second argument of '{node.Name}' must be a literal value.");
                }

                var builder = new StringBuilder();
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Append(prefix);
                }

                builder.Append(literal.Value.ToString());

                if (!string.IsNullOrEmpty(suffix))
                {
                    builder.Append(suffix);
                }

                var likeValue = builder.ToString();
                var parameter = CreateParameter(likeValue);
                var operatorKeyword = _dialect == Dialect.Postgres ? "ILIKE" : "LIKE";
                return $"({QuoteQualifiedIdentifier(property.Name, _dialect)} {operatorKeyword} {parameter})";
            }

            private string CreateParameter(object? value)
            {
                var name = NextParameterName(_parameterIndex++);
                _parameters.Add(name, value);
                return $"@{name}";
            }

            private static bool IsNullLiteral(FilterNode node)
                => node is LiteralFilterNode literal && literal.Value is null;
        }
    }
}
