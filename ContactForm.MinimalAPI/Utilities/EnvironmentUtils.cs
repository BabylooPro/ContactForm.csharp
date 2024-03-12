using System;
using System.Collections.Generic;

namespace ContactForm.MinimalAPI.Utilities
{
    // UTILITY CLASS FOR ENVIRONMENT VARIABLES
    public static class EnvironmentUtils
    {
        public static List<string> CheckMissingEnvironmentVariables(params string[] variableNames)
        {
            var missingVariables = new List<string>(); // LIST FOR STORING MISSING VARIABLES

            // CHECKING FOR MISSING ENVIRONMENT VARIABLES
            foreach (var name in variableNames)
            {
                var variableValue = Environment.GetEnvironmentVariable(name); // GETTING ENVIRONMENT VARIABLE VALUE

                // ADDING MISSING VARIABLE TO THE LIST
                if (string.IsNullOrWhiteSpace(variableValue))
                {
                    missingVariables.Add(name);
                }
            }

            return missingVariables; // RETURNING MISSING VARIABLES
        }
    }
}
