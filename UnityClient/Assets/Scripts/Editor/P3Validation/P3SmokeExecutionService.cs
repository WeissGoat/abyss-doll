using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class P3SmokeExecutionResult { public bool Passed=true; public List<string> Tests=new List<string>(); public List<string> Logs=new List<string>(); public List<string> Errors=new List<string>(); }
public static class P3SmokeExecutionService {
    public static P3SmokeExecutionResult Execute(IEnumerable<string> testNames) {
        var result=new P3SmokeExecutionResult(); Application.LogCallback h=(c,s,t)=>{ result.Logs.Add("["+t+"] "+c); if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert){result.Passed=false;result.Errors.Add(c);} }; Application.logMessageReceived+=h;
        try { foreach(var name in testNames){ result.Tests.Add(name); try { int split=name.LastIndexOf('.'); if(split<1) throw new InvalidOperationException("Invalid test name"); Type type=null; foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){type=a.GetType(name.Substring(0,split));if(type!=null)break;} var method=type?.GetMethod(name.Substring(split+1),BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic); if(method==null) throw new MissingMethodException(name); method.Invoke(null,null); } catch(Exception ex){result.Passed=false;result.Errors.Add((ex.InnerException??ex).Message);} } }
        finally { Application.logMessageReceived-=h; }
        return result;
    }
}
