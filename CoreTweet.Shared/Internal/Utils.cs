// The MIT License (MIT)
//
// CoreTweet - A .NET Twitter Library supporting Twitter API 1.1
// Copyright (c) 2014 lambdalice
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;

#if !NET35
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LibAzyotter.Internal
{
    internal static class InternalUtils
    {
        internal static IEnumerable<KeyValuePair<string, object>> ResolveObject(object t)
        {
            if(t == null)
                return new Dictionary<string, object>();
            if(t is IEnumerable<KeyValuePair<string, object>>)
                return t as IEnumerable<KeyValuePair<string, object>>;

            var type = t.GetType().GetTypeInfo();

            if(type.GetCustomAttributes(typeof(TwitterParametersAttribute), false).Any())
            {
                var d = new Dictionary<string, object>();

                foreach(var f in type.DeclaredFields.Where(x => x.IsPublic && !x.IsStatic))
                {
                    var attr = (TwitterParameterAttribute)f.GetCustomAttributes(true).FirstOrDefault(y => y is TwitterParameterAttribute);
                    var value = f.GetValue(t);
                    if(attr.DefaultValue == null)
                        attr.DefaultValue = GetDefaultValue(t.GetType());

                    if(attr != null && value != null && !value.Equals(attr.DefaultValue))
                    {
                        var name = attr.Name;
                        d.Add(name != null ? name : f.Name, value);
                    }
                }

                foreach(var p in type.DeclaredProperties.Where(x => x.CanRead && x.GetMethod.IsPublic && !x.GetMethod.IsStatic))
                {
                    var attr = (TwitterParameterAttribute)p.GetCustomAttributes(true).FirstOrDefault(y => y is TwitterParameterAttribute);
                    var value = p.GetValue(t, null);
                    if(attr.DefaultValue == null)
                        attr.DefaultValue = GetDefaultValue(t.GetType());

                    if(attr != null && value != null && !value.Equals(attr.DefaultValue))
                    {
                        var name = attr.Name;
                        d.Add(name != null ? name : p.Name, value);
                    }
                }

                return d;
            }
            else
            {
                // IEnumerable<KeyVakuePair<string, Any>> or IEnumerable<Tuple<string, Any>>
                var ienumerable = t as System.Collections.IEnumerable;
                if(ienumerable != null)
                {
                    var elements = ienumerable.Cast<object>();
                    var ieElementTypes =
                        type.GetInterfaces()
                        .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        .Select(x => x.GenericTypeArguments[0].GetTypeInfo())
                        .Where(x => x.IsGenericType && x.GenericTypeArguments[0] == typeof(string));
                    foreach(var genericElement in ieElementTypes)
                    {
                        var genericTypeDefinition = genericElement.GetGenericTypeDefinition();
                        if(genericTypeDefinition == typeof(KeyValuePair<,>))
                        {
                            var getKey = genericElement.GetProperty("Key").GetGetMethod();
                            var getValue = genericElement.GetProperty("Value").GetGetMethod();
                            return elements.Select(x => new KeyValuePair<string, object>(
                                getKey.Invoke(x, null) as string,
                                getValue.Invoke(x, null)
                            ));
                        }
#if !NET35
                        else if(genericTypeDefinition == typeof(Tuple<,>))
                        {
                            var getItem1 = genericElement.GetProperty("Item1").GetGetMethod();
                            var getItem2 = genericElement.GetProperty("Item2").GetGetMethod();
                            return elements.Select(x => new KeyValuePair<string, object>(
                                getItem1.Invoke(x, null) as string,
                                getItem2.Invoke(x, null)
                            ));
                        }
#endif
                    }
                }

                return AnnoToDictionary(t);
            }
        }

        private static IDictionary<string,object> AnnoToDictionary<T>(T f)
        {
            return typeof(T).GetRuntimeProperties()
                .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
                .Select(x => Tuple.Create(x.Name, x.GetMethod))
                .Where(x => x.Item2.IsPublic && !x.Item2.IsStatic)
                .ToDictionary(x => x.Item1, x => x.Item2.Invoke(f, null));
        }

        private static object GetExpressionValue(Expression<Func<string,object>> expr)
        {
            var constExpr = expr.Body as ConstantExpression;
            return constExpr != null ? constExpr.Value : expr.Compile()("");
        }

        private static object GetDefaultValue(Type type)
        {
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }

        internal static IEnumerable<KeyValuePair<string, object>> ExpressionsToDictionary(IEnumerable<Expression<Func<string,object>>> exprs)
        {
            return exprs.Select(x => new KeyValuePair<string, object>(x.Parameters[0].Name, GetExpressionValue(x)));
        }

        internal static readonly DateTimeOffset unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

        internal static DateTimeOffset GetUnixTime(long seconds)
        {
            return unixEpoch.AddTicks(checked(seconds * 10000000));
        }

        internal static DateTimeOffset GetUnixTimeMs(long milliseconds)
        {
            return unixEpoch.AddTicks(checked(milliseconds * 10000));
        }

        private const string XRateLimitLimit = "x-rate-limit-limit";
        private const string XRateLimitRemaining = "x-rate-limit-remaining";
        private const string XRateLimitReset = "x-rate-limit-reset";
        
#if !NET35
        internal static RateLimit ReadRateLimit(HttpResponseMessage response)
        {
            var h = response.Headers;
            IEnumerable<string> limit, remaining, reset;
            if (h.TryGetValues(XRateLimitLimit, out limit)
                && h.TryGetValues(XRateLimitRemaining,out remaining)
                && h.TryGetValues(XRateLimitReset, out reset))
            {
                return new RateLimit()
                {
                    Limit = int.Parse(limit.Single()),
                    Remaining = int.Parse(remaining.Single()),
                    Reset = GetUnixTime(long.Parse(reset.Single()))
                };
            }

            return null;
        }
#endif

        private static KeyValuePair<string, object> GetReservedParameter(List<KeyValuePair<string, object>> parameters, string reserved)
        {
            return parameters.Single(kvp => kvp.Key == reserved);
        }

#if false
        /// <summary>
        /// id, slug, etc
        /// </summary>
        internal static T AccessParameterReservedApi<T>(this TokensBase t, MethodType m, string uri, string reserved, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if(parameters == null) throw new ArgumentNullException("parameters");
            var list = parameters.ToList();
            var kvp = GetReservedParameter(list, reserved);
            list.Remove(kvp);
            return t.AccessApiImpl<T>(m, uri.Replace(string.Format("{{{0}}}", reserved), kvp.Value.ToString()), list, "");
        }

        internal static ListedResponse<T> AccessParameterReservedApiArray<T>(this TokensBase t, MethodType m, string uri, string reserved, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if(parameters == null) throw new ArgumentNullException("parameters");
            var list = parameters.ToList();
            var kvp = GetReservedParameter(list, reserved);
            list.Remove(kvp);
            return t.AccessApiArrayImpl<T>(m, uri.Replace(string.Format("{{{0}}}", reserved), kvp.Value.ToString()), list, "");
        }
#endif

#if !NET35
        internal static Task<T> AccessParameterReservedApiAsync<T>(this TwitterClient t, HttpMethod m, string uri, string reserved, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            if(parameters == null) throw new ArgumentNullException("parameters");
            var list = parameters.ToList();
            var kvp = GetReservedParameter(list, reserved);
            list.Remove(kvp);
            return t.AccessApiAsyncImpl<T>(m, uri.Replace(string.Format("{{{0}}}", reserved), kvp.Value.ToString()), list, cancellationToken, "");
        }

        internal static Task<ListedResponse<T>> AccessParameterReservedApiArrayAsync<T>(this TwitterClient t, HttpMethod m, string uri, string reserved, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            if(parameters == null) throw new ArgumentNullException("parameters");
            var list = parameters.ToList();
            var kvp = GetReservedParameter(list, reserved);
            list.Remove(kvp);
            return t.AccessApiArrayAsyncImpl<T>(m, uri.Replace(string.Format("{{{0}}}", reserved), kvp.Value.ToString()), list, cancellationToken, "");
        }
#endif
    }
}
