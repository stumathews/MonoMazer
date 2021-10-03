//-----------------------------------------------------------------------

// <copyright file="ConditionNotSatisfiedFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class ConditionNotSatisfiedFailure : IFailure
    {
        public ConditionNotSatisfiedFailure(string caller = "Caller not captured")
        {
            Reason = $"Condition not satisfied. Caller: {caller}";
        }
        public string Reason { get; set; }
        public static IFailure Create(string caller) => new ConditionNotSatisfiedFailure(caller);
    }
}
