using System;
using System.Linq;
using Xunit;

namespace Redis.OM.Unit.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SkipIfMissingEnvVarAttribute : FactAttribute
{
    private readonly string[] _envVars;

    public SkipIfMissingEnvVarAttribute(params string[] envVars)
    {
        _envVars = envVars;
    }

    public override string Skip
    {
        get
        {
            var missingEnvVars = _envVars.Where(x => Environment.GetEnvironmentVariable(x) == null).ToArray();
            if (missingEnvVars.Any())
            {
                return $"Skipping because the following environment variables were missing: {string.Join(",", missingEnvVars)}";
            }

            return null;
        }
    }
}