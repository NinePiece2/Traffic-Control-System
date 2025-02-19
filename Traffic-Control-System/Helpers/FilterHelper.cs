using System.Linq.Expressions;
using Traffic_Control_System.Models;

namespace Traffic_Control_System{
    public static class FilterHelper
    {
        public static Expression<Func<T, bool>> ApplyFilters<T>(List<Filter> filters, string condition)
        {
            if (filters == null || filters.Count == 0) return x => true; // Return a default "always true" expression
            
            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            Expression finalExpression = null;

            foreach (var filter in filters)
            {
                Expression filterExpression = BuildFilterExpression<T>(parameter, filter);

                if (filterExpression == null) continue;

                if (finalExpression == null)
                {
                    finalExpression = filterExpression;
                }
                else
                {
                    finalExpression = condition.ToLower() == "or"
                        ? Expression.OrElse(finalExpression, filterExpression)
                        : Expression.AndAlso(finalExpression, filterExpression);
                }
            }

            return Expression.Lambda<Func<T, bool>>(finalExpression ?? Expression.Constant(true), parameter);
        }

        public class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }

        public static Expression BuildFilterExpression<T>(ParameterExpression parameter, Filter filter)
        {
            if (filter.IsComplex)
            {
                var innerLambda = ApplyFilters<T>(filter.Predicates, filter.Condition);
                var replacedBody = new ParameterReplacer(innerLambda.Parameters[0], parameter).Visit(innerLambda.Body);
                return replacedBody;
            }

            var property = typeof(T).GetProperty(filter.Field);
            if (property == null) return null;

            var propertyAccess = Expression.Property(parameter, property);
            var filterValue = Convert.ChangeType(filter.Value, property.PropertyType);
            var constant = Expression.Constant(filterValue);

            return filter.Operator.ToLower() switch
            {
                "eq" => Expression.Equal(propertyAccess, constant),
                "greaterthan" => Expression.GreaterThan(propertyAccess, constant),
                "lessthan" => Expression.LessThan(propertyAccess, constant),
                "contains" => Expression.Call(propertyAccess, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant),
                "startswith" => Expression.Call(propertyAccess, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant),
                "endswith" => Expression.Call(propertyAccess, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant),
                _ => null
            };
        }
    }
}