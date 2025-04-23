using CSharpFunctionalExtensions;

namespace UletiSmenu.Tests.TestHelpers
{
    public static class HelperMethods
    {
        public static T EnsureSuccess<T>(Result<T> result)
        {
            if (result.IsFailure)
                throw new Exception($"Factory failed: {result.Error}");
            return result.Value;
        }
    }
}
