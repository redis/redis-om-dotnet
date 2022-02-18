using System;
using System.Threading;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    internal class Helper
    {
        internal static void RunTestUnderDifferentCulture(string lcid, Action<object> test)
        {
            if (lcid == null)
                throw new ArgumentNullException(nameof(lcid));

            if (test == null)
                throw new ArgumentNullException(nameof(lcid));

            var differentCulture = new System.Globalization.CultureInfo(lcid);

            Assert.NotEqual(".", differentCulture.NumberFormat.NumberDecimalSeparator);

            // set a different culture for the current thread
            Thread.CurrentThread.CurrentCulture = differentCulture;
            Thread.CurrentThread.CurrentUICulture = differentCulture;

            test.Invoke(null);
        }
    }
}
