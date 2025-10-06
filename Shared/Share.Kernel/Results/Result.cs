using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Kernel.Results
{
    public class Result
    {
        public bool IsSuccess { get; }
        public Error? Error { get; }

        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(string code, string message) =>
            new(false, new Error(code, message));
        public static Result Failure(Error error) =>
    new(false, error ?? throw new ArgumentNullException(nameof(error)));
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value, Error? error)
            : base(isSuccess, error)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static new Result<T> Failure(string code, string message) =>
            new(false, default, new Error(code, message));
        public static Result<T> Failure(Error error) =>
    new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
    }
}
