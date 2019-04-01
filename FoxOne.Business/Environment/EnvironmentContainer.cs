using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using FoxOne.Core;
using System.Text.RegularExpressions;
namespace FoxOne.Business.Environment
{
    public class EnvironmentContainer
    {
        private readonly IList<IEnvironmentProvider> _providers = TypeHelper.GetAllImplInstance<IEnvironmentProvider>();
        public static readonly Regex Pattern = new Regex(@"\$(?<Variable>.+?)\$", RegexOptions.Compiled);
        public string Parse(string expression)
        {
            if (string.IsNullOrEmpty(expression) || Pattern.Matches(expression).Count==0)
            {
                return expression;
            }
            StringBuilder builder = new StringBuilder();
            var arr = Pattern.Split(expression);
            object value;
            for(int i=0;i<arr.Length;i++)
            {
                if (TryResolve(arr[i], out value))
                {
                    builder.Append(value == null ? string.Empty : value.ToString());
                }
                else
                {
                    builder.Append(arr[i]);
                }
            }
            return builder.ToString();
        }
        public virtual bool TryResolve(string expression, out object value)
        {
            expression = expression.Trim('$');
            value = expression;
            if (expression.IsNullOrEmpty()) return false;
            IEnvironmentProvider provider = null;
            int dotIndex = expression.IndexOf(".");
            if (dotIndex < 0)
            {
                provider = new DefaultProvider();
            }
            else
            {
                string prefix = expression.Substring(0, dotIndex);
                expression = expression.Substring(dotIndex + 1);
                provider = _providers.FirstOrDefault(o => o.Prefix.Split('|').Contains(prefix, StringComparer.OrdinalIgnoreCase));
            }
            if (provider != null)
            {
                value = provider.Resolve(expression);
                if(value==null)
                {
                    return false;
                }
                return true;
            }
            value = null;
            return false;
        }
    }
}