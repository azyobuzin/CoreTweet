﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RestApisGen
{
    public class ApiEndpoint
    {
        private static readonly string[] valueTypes = new[] { "int", "long", "byte", "double", "bool" };

        public string Name { get; set; }

        public string Request { get; set; }

        public string ReturnType { get; set; }

        public ApiType Type { get; set; }

        public string ReservedName { get; set; }

        public string JsonPath { get; set; }

        public Tuple<string, string>[] Attributes { get; set; }

        public string Uri { get; set; }

        public string[] Description { get; set; }

        public string Returns { get; set; }

        public Parameter[] Params = new Parameter[0];

        public string[][] CustomBodies = new string[8][];

        public string[] OmitExcept = new string[0];

        public List<List<Parameter[]>> AnyOneGroups = new List<List<Parameter[]>>();
        
        public string MethodDefinitionAsync
        {
            get
            {
                switch (this.Type)
                {
                    case ApiType.Void:
                        return string.Format("public Task {0}Async", this.Name);
                    case ApiType.Normal:
                        return string.Format("public Task<{0}> {1}Async", this.ReturnType, this.Name);
                    case ApiType.Listed:
                        return string.Format("public Task<ListedResponse<{0}>> {1}Async", this.ReturnType, this.Name);
                    case ApiType.Cursored:
                        return string.Format("public Task<Cursored<{0}>> {1}Async", this.ReturnType, this.Name);
                    case ApiType.Dictionary:
                        return string.Format("public Task<DictionaryResponse<string, {0}>> {1}Async", this.ReturnType, this.Name);
                    default:
                        throw new ArgumentException("");
                }
            }
        }

        public string JsonPathOrEmpty { get { return JsonPath != null ? ", " + "\"" + JsonPath + "\"" : ""; } }

        string FormatWith(int i, string s, params object[] args)
        {
            if (CustomBodies[i] != null)
                return string.Join(Environment.NewLine, CustomBodies[i]);
            else
                return string.Format(s, args);
        }

        public Method PEAsync
        {
            get
            {
                var s1 = this.MethodDefinitionAsync + "(params Expression<Func<string, object>>[] parameters)";
                var s2 = "";
                if (this.ReservedName == null)
                    switch (this.Type)
                    {
                        case ApiType.Void:
                            s2 = FormatWith(4, "return this.Tokens.AccessApiNoResponseAsync(\"{0}\", parameters);", this.Uri);
                            break;
                        case ApiType.Normal:
                            s2 = FormatWith(4, "return this.Tokens.AccessApiAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(4, "return this.Tokens.AccessApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Cursored:
                            s2 = FormatWith(4, "return this.Tokens.AccessApiAsync<Cursored<{0}>>(HttpMethod.{1}, \"{2}\", parameters{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Dictionary:
                            s2 = FormatWith(4, "return this.Tokens.AccessApiDictionaryAsync<string, {0}>(HttpMethod.{1}, \"{2}\", parameters{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                    }
                else
                {
                    switch (this.Type)
                    {
                        case ApiType.Normal:
                            s2 = FormatWith(4,
                                "return this.Tokens.AccessParameterReservedApiAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(4,
                                "return this.Tokens.AccessParameterReservedApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                    }
                }
                return new Method(s1, this.Params, new[] { s2 });
            }
        }

        public Method IDAsync
        {
            get
            {
                var s1 = this.MethodDefinitionAsync + "(IDictionary<string, object> parameters, CancellationToken cancellationToken = default(CancellationToken))";
                var s2 = "";
                if (this.ReservedName == null)
                    switch (this.Type)
                    {
                        case ApiType.Void:
                            s2 = FormatWith(5, "return this.Tokens.AccessApiNoResponseAsync(\"{0}\", parameters, cancellationToken);", this.Uri);
                            break;
                        case ApiType.Normal:
                            s2 = FormatWith(5, "return this.Tokens.AccessApiAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(5, "return this.Tokens.AccessApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Cursored:
                            s2 = FormatWith(5, "return this.Tokens.AccessApiAsync<Cursored<{0}>>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Dictionary:
                            s2 = FormatWith(5, "return this.Tokens.AccessApiDictionaryAsync<string, {0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                    }
                else
                {
                    switch (this.Type)
                    {
                        case ApiType.Normal:
                            s2 = FormatWith(5,
                                "return this.Tokens.AccessParameterReservedApiAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", parameters, cancellationToken);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(5,
                                "return this.Tokens.AccessParameterReservedApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", parameters, cancellationToken);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                    }
                }
                return new Method(s1, this.Params, new[] { s2 }, takesCancellationToken: true);
            }
        }

        public Method TAsync
        {
            get
            {
                var s1 = this.MethodDefinitionAsync + "(object parameters, CancellationToken cancellationToken = default(CancellationToken))";
                var s2 = "";
                if (this.ReservedName == null)
                    switch (this.Type)
                    {
                        case ApiType.Void:
                            s2 = FormatWith(6, "return this.Tokens.AccessApiNoResponseAsync(\"{0}\", parameters, cancellationToken);", this.Uri);
                            break;
                        case ApiType.Normal:
                            s2 = FormatWith(6, "return this.Tokens.AccessApiAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(6, "return this.Tokens.AccessApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Cursored:
                            s2 = FormatWith(6, "return this.Tokens.AccessApiAsync<Cursored<{0}>>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Dictionary:
                            s2 = FormatWith(6, "return this.Tokens.AccessApiDictionaryAsync<string, {0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                    }
                else
                {
                    switch (this.Type)
                    {
                        case ApiType.Normal:
                            s2 = FormatWith(6,
                                "return this.Tokens.AccessParameterReservedApiAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", InternalUtils.ResolveObject(parameters), cancellationToken);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(6,
                                "return this.Tokens.AccessParameterReservedApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", InternalUtils.ResolveObject(parameters), cancellationToken);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                    }
                }
                return new Method(s1, this.Params, new[] { s2 }, takesCancellationToken: true);
            }
        }

        public Method[] StaticAsync
        {
            get
            {
                var eithered = Extensions.Combinate(this.AnyOneGroups);
                var uneithered = this.Params.Where(x => !AnyOneGroups.SelectMany(_ => _).SelectMany(_ => _).Contains(x));
                var rs = new List<Method>();

                var s2 = "";
                if (this.ReservedName == null)
                    switch (this.Type)
                    {
                        case ApiType.Void:
                            s2 = FormatWith(7, "return this.Tokens.AccessApiNoResponseAsync(\"{0}\", parameters, cancellationToken);", this.Uri);
                            break;
                        case ApiType.Normal:
                            s2 = FormatWith(7, "return this.Tokens.AccessApiAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(7, "return this.Tokens.AccessApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Cursored:
                            s2 = FormatWith(7, "return this.Tokens.AccessApiAsync<Cursored<{0}>>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                        case ApiType.Dictionary:
                            s2 = FormatWith(7, "return this.Tokens.AccessApiDictionaryAsync<string, {0}>(HttpMethod.{1}, \"{2}\", parameters, cancellationToken{3});", this.ReturnType, this.Request, this.Uri, JsonPathOrEmpty);
                            break;
                    }
                else
                {
                    switch (this.Type)
                    {
                        case ApiType.Normal:
                            s2 = FormatWith(7,
                                "return this.Tokens.AccessParameterReservedApiAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", parameters, cancellationToken);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                        case ApiType.Listed:
                            s2 = FormatWith(7,
                                "return this.Tokens.AccessParameterReservedApiArrayAsync<{0}>(HttpMethod.{1}, \"{2}\", \"{3}\", parameters, cancellationToken);"
                                , this.ReturnType, this.Request, this.Uri, this.ReservedName);
                            break;
                    }
                }

                if (eithered.Count() != 0)
                    foreach (var x in eithered)
                    {
                        var o = this.Params.Where(y => !this.AnyOneGroups.SelectMany(_ => _).SelectMany(_ => _).Contains(y) || x.SelectMany(_ => _).Contains(y));
                        var s1 = this.MethodDefinitionAsync + "(" +
                            string.Join(", ",
                                o.Select(y =>
                                    (y.IsOptional && valueTypes.Contains(y.Type)
                                        ? y.Type + "?"
                                        : y.Type)
                                    + " " + y.Name + (y.IsOptional ? " = null" : ""))
                                .Concat(new[] { "CancellationToken cancellationToken = default(CancellationToken))" }));
                        var prmps = new List<string>();
                        prmps.Add("var parameters = new Dictionary<string, object>();");
                        foreach (var y in o)
                        {
                            if (y.IsOptional)
                                prmps.Add(string.Format("if({0} != null) parameters.Add(\"{1}\", {0});", y.Name, y.RealName));
                            else if (valueTypes.Contains(y.Type))
                                prmps.Add(string.Format("parameters.Add(\"{1}\", {0});", y.Name, y.RealName));
                            else
                            {
                                prmps.Add(string.Format("if({0} == null) throw new ArgumentNullException(\"{1}\");", y.Name, y.RealName));
                                prmps.Add(string.Format("else parameters.Add(\"{1}\", {0});", y.Name, y.RealName));
                            }
                        }
                        prmps.Add(s2);
                        rs.Add(new Method(s1, o, prmps.ToArray(), true, true));
                    }
                else
                {
                    var s1 = this.MethodDefinitionAsync + "(" +
                        string.Join(", ",
                            uneithered.Select(y =>
                                (y.IsOptional && valueTypes.Contains(y.Type)
                                    ? y.Type + "?"
                                    : y.Type)
                                + " " + y.Name + (y.IsOptional ? " = null" : ""))) + (uneithered.Count() != 0 ? ", " : "") + "CancellationToken cancellationToken = default(CancellationToken))";
                    var prmps = new List<string>();
                    prmps.Add("var parameters = new Dictionary<string, object>();");

                    foreach (var y in uneithered)
                        if (y.IsOptional)
                            prmps.Add(string.Format("if({0} != null) parameters.Add(\"{1}\", {0});", y.Name, y.RealName));
                        else if (valueTypes.Contains(y.Type))
                            prmps.Add(string.Format("parameters.Add(\"{1}\", {0});", y.Name, y.RealName));
                        else
                        {
                            prmps.Add(string.Format("if({0} == null) throw new ArgumentNullException(\"{1}\");", y.Name, y.RealName));
                            prmps.Add(string.Format("else parameters.Add(\"{1}\", {0});", y.Name, y.RealName));
                        }

                    prmps.Add(s2);
                    rs.Add(new Method(s1, uneithered, prmps.ToArray(), true, true));
                }
                return rs.ToArray();
            }
        }

        public Dictionary<string, Method[]> MethodAsyncDic
        {
            get
            {
                var dic = new Dictionary<string, Method[]>();
                dic.Add("asyncpe", new[] { this.PEAsync });
                dic.Add("asyncid", new[] { this.IDAsync });
                dic.Add("asynct", new[] { this.TAsync });
                dic.Add("asyncstatic", this.StaticAsync);
                return dic;
            }
        }

        public IEnumerable<Method> MethodsAsync
        {
            get
            {
                if (this.OmitExcept.Length == 0)
                    return MethodAsyncDic.Select(x => x.Value).SelectMany(x => x);
                else
                    return this.OmitExcept.Where(x => MethodAsyncDic.ContainsKey(x)).Select(x => MethodAsyncDic[x]).SelectMany(x => x);
            }
        }
    }

    public class Parameter
    {
        public string Kind { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string When { get; set; }
        public bool IsOptional { get { return this.Kind.IndexOf("optional", StringComparison.OrdinalIgnoreCase) != -1; } }
        public string RealName { get { return this.Name.TrimStart('@'); } }

        public Parameter(string kind, string type, string name, string @when)
        {
            this.Kind = kind;
            this.Type = type;
            this.Name = name;
            this.When = @when;
        }
    }

    public class ParameterNameComparer : IEqualityComparer<Parameter>
    {
        public bool Equals(Parameter x, Parameter y)
        {
            if (x == null)
                return y == null;
            if (y == null)
                return false;
            return x.Name == y.Name && x.Type == y.Type;
        }

        public int GetHashCode(Parameter x)
        {
            return x == null ? 0 : (x.Name + x.Type).GetHashCode();
        }
    }

    public class Method
    {
        public string Definition { get; set; }
        public Parameter[] Params { get; set; }
        public string[] Body { get; set; }
        public bool HasStaticArgs { get; set; }
        public bool TakesCancellationToken { get; set; }

        public string WhenClause
        {
            get
            {
                return this.HasStaticArgs
                    ? this.Params.Select(p => p.When).SingleOrDefault(x => !string.IsNullOrEmpty(x))
                    : null;
            }
        }

        public Method(string definition, IEnumerable<Parameter> parameters, string[] body, bool hasStaticArgs = false, bool takesCancellationToken = false)
        {
            this.Definition = definition;
            this.Params = parameters != null ? parameters.ToArray() : new Parameter[0];
            this.Body = body;
            this.HasStaticArgs = hasStaticArgs;
            this.TakesCancellationToken = takesCancellationToken;
        }

        public override string ToString()
        {
            return this.Definition + Environment.NewLine + string.Join(Environment.NewLine, this.Body);
        }
    }

    public class RawLines : ApiEndpoint
    {
        public string[] Lines { get; set; }
    }

    public enum ApiType
    {
        Normal,
        Listed,
        Cursored,
        Dictionary,
        Void
    }

    public class Indent
    {
        int indent { get; set; }

        public int Spaces { get; set; }

        public Indent(int i, int s = 4)
        {
            this.indent = i;
            this.Spaces = s;
        }

        public void Inc()
        {
            indent = indent + 1;
        }

        public void Dec()
        {
            indent = indent - 1;
        }

        public override string ToString()
        {
            return new string(' ', Spaces * indent);
        }
    }

    public enum Mode
    {
        none,
        endpoint,
        description,
        returns,
        prms,
        with,
        pe,
        id,
        t,
        stat,
        ape,
        aid,
        at,
        astat
    }

    public class ApiParent
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public ApiEndpoint[] Endpoints { get; set; }

        public static ApiParent Parse(string fileName)
        {
            var ret = new ApiParent();

            var lines = File.ReadAllLines(fileName);
            ret.Name = lines.First(x => x.StartsWith("#namespace")).Split(' ')[1];
            ret.Description = lines.First(x => x.StartsWith("#description")).Replace("#description ", "");

            var es = new List<ApiEndpoint>();

            var mode = Mode.none;
            var now = new ApiEndpoint();
            var s = new List<string>();
            var s2 = new List<string>();
            var commenting = false;
            var cbs = new string[][] { null, null, null, null, null, null, null, null };
            var ats = new List<Tuple<string, string>>();
            var ang = new Dictionary<int, List<Parameter[]>>();

            foreach (var i in lines)
            {
                var l = i.TrimStart('\t', ' ');
                if (l.StartsWith("/*") || l.StartsWith("#raw"))
                {
                    commenting = true;
                }
                else if (l.StartsWith("#endraw") || l.StartsWith("*/"))
                {
                    commenting = false;
                    if (l.StartsWith("#endraw"))
                    {
                        es.Add(new RawLines() { Lines = s2.ToArray() });
                    }
                    s2.Clear();
                }
                else if (commenting)
                {
                    s2.Add(i);
                }
                else if (l.StartsWith("endpoint"))
                {
                    var x = l.Split(' ');
                    now.Name = x[2];
                    var rt = x[1];
                    if (rt.StartsWith("void"))
                    {
                        now.ReturnType = "void";
                        now.Type = ApiType.Void;
                    }
                    else if (rt.StartsWith("Listed"))
                    {
                        now.ReturnType = rt.Split(new[] { '<', '>' })[1];
                        now.Type = ApiType.Listed;
                    }
                    else if (rt.StartsWith("Cursored"))
                    {
                        now.ReturnType = rt.Split(new[] { '<', '>' })[1];
                        now.Type = ApiType.Cursored;
                    }
                    else if (rt.StartsWith("Dictionary"))
                    {
                        now.ReturnType = rt.Substring(11, rt.Length - 12);
                        now.Type = ApiType.Dictionary;
                    }
                    else
                    {
                        now.ReturnType = x[1];
                        now.Type = ApiType.Normal;
                    }
                    now.Request = x[4];
                    now.Uri = x[5];
                    if (now.Uri.Contains("{"))
                    {
                        now.ReservedName = now.Uri.Split(new[] { '{', '}' })[1];
                    }
                    mode = Mode.endpoint;
                }
                else if (l.StartsWith("description"))
                {
                    mode = Mode.description;
                }
                else if (l.StartsWith("returns"))
                {
                    mode = Mode.returns;
                }
                else if (l.StartsWith("params"))
                {
                    mode = Mode.prms;
                }
                else if (l.StartsWith("with"))
                {
                    mode = Mode.with;
                }
                else if (l.StartsWith("pe"))
                {
                    mode = Mode.pe;
                }
                else if (l.StartsWith("id"))
                {
                    mode = Mode.id;
                }
                else if (l.StartsWith("t"))
                {
                    mode = Mode.t;
                }
                else if (l.StartsWith("static"))
                {
                    mode = Mode.stat;
                }
                else if (l.StartsWith("asyncpe"))
                {
                    mode = Mode.ape;
                }
                else if (l.StartsWith("asyncid"))
                {
                    mode = Mode.aid;
                }
                else if (l.StartsWith("asynct"))
                {
                    mode = Mode.at;
                }
                else if (l.StartsWith("asyncstatic"))
                {
                    mode = Mode.astat;
                }
                else if (l.StartsWith("{"))
                {
                }
                else if (l.StartsWith("}"))
                    switch (mode)
                    {
                        case Mode.none:
                            break;
                        case Mode.description:
                            now.Description = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.returns:
                            now.Returns = string.Join(Environment.NewLine, s);
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.prms:
                            now.Params = s.Select(x =>
                                {
                                    var y = x.Split(' ');
                                    if (y[0].StartsWith("either"))
                                    {
                                        var j = y[0].Split(new[] { '[', ']' });
                                        var index = j.Length > 1 ? int.Parse(j[1]) : 0;
                                        var prms = y.Length == 1 ? new Parameter[0]
                                            : x.Substring(x.IndexOf(' ')).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(z =>
                                            {
                                                var e = z.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                                var @when = e.Length >= 4 && e[2] == "when" ? z.Substring(z.LastIndexOf(" when ") + 6) : null;
                                                return z.Length == 0 ? null : new Parameter("any one is required", e[0], e[1], @when);
                                            })
                                            .ToArray();
                                        if (!ang.ContainsKey(index))
                                            ang[index] = new List<Parameter[]>();
                                        ang[index].Add(prms);
                                        return prms;
                                    }
                                    else
                                        return new[] { new Parameter(y[0], y[1], y[2], null) };
                                }).SelectMany(_ => _).Where(x => x != null).ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.with:
                            foreach (var x in s)
                            {
                                if (x.StartsWith("JsonPath="))
                                {
                                    now.JsonPath = x.Replace("JsonPath=", "");
                                }
                                else if (x.StartsWith("OmitExcept="))
                                {
                                    now.OmitExcept = x.Replace("OmitExcept=", "").Split(',');
                                }
                                else if (x.StartsWith("["))
                                {
                                    var name = x.Split(new[] { '[', ']' })[1];
                                    ats.Add(Tuple.Create(name, x.Replace("[" + name + "]=", "")));
                                }
                            }
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.pe:
                            cbs[0] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.id:
                            cbs[1] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.t:
                            cbs[2] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.stat:
                            cbs[3] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.ape:
                            cbs[4] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.aid:
                            cbs[5] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.at:
                            cbs[6] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.astat:
                            cbs[7] = s.ToArray();
                            s.Clear();
                            mode = Mode.endpoint; break;
                        case Mode.endpoint:
                            now.CustomBodies = cbs;
                            now.Attributes = ats.ToArray();
                            now.AnyOneGroups = ang.Values.ToList();
                            es.Add(now);

                            now = new ApiEndpoint();
                            mode = Mode.none;
                            s.Clear();
                            cbs = new string[][] { null, null, null, null, null, null, null, null };
                            ats.Clear();
                            ang.Clear();
                            break;
                    }
                else if (!l.StartsWith("#") && !l.StartsWith("//") && !l.All(x => char.IsWhiteSpace(x)) && l != "")
                    s.Add(l);
            }

            ret.Endpoints = es.ToArray();

            return ret;
        }
    }

    public static class Extensions
    {
        static IEnumerable<IEnumerable<T>> Combinate<T>(IEnumerable<IEnumerable<T>> x, IEnumerable<T> y)
        {
            foreach (var a in x)
                foreach (var b in y)
                    yield return a.Concat(new[] { b });
        }

        static IEnumerable<IEnumerable<T>> Flatten<T>(IEnumerable<IEnumerable<T>> x)
        {
            foreach (var i in x)
                foreach (var j in i)
                    yield return new[] { j };
        }

        public static IEnumerable<IEnumerable<T>> Combinate<T>(IEnumerable<IEnumerable<T>> x)
        {
            if (x.Count() < 2)
                return Flatten(x);
            var y = Flatten(x.Take(1));

            foreach (var i in x.Skip(1))
                y = Combinate(y, i);
            return y;
        }
    }
}
