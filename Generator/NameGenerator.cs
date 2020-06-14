using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace TopLearn.Core.Generator
{
    public class NameGenerator
    {
        public static string GenerateUniqCode()
        {


            return Guid.NewGuid().ToString().Replace("-", "");
        }
    }
}
