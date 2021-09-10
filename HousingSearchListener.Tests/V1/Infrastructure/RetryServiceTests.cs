﻿using FluentAssertions;
using HousingSearchListener.V1.Infrastructure;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace HousingSearchListener.Tests.V1.Infrastructure
{
    public class RetryServiceTests
    {
        [Fact]
        public async void DoAsyncWithValidData()
        {
            var result = await RetryService.DoAsync(CreateTask(), maxAttemptCount: 5, delay: TimeSpan.FromSeconds(2));

            result.Should().NotBeNull();
            result.Should().Be("Task result.");
        }

        [Fact]
        public void DoAsyncNullActionThrowsArgumentNullException()
        {
            Func<Task> action = async () => await RetryService.DoAsync<HttpResponseMessage>(null, maxAttemptCount: 5, delay: TimeSpan.FromSeconds(2));

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DoAsyncWithNoneAttemptsShouldthrowsAggregateException()
        {
            Func<Task> action = async () => await RetryService.DoAsync(CreateTask(), maxAttemptCount: 0, delay: TimeSpan.FromSeconds(2));

            action.Should().Throw<AggregateException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15.1)]
        [InlineData(300)]
        [InlineData(1000)]
        public async void DoAsyncWithDifferenceDelay(double milliseconds)
        {
            var result = await RetryService.DoAsync(CreateTask(), maxAttemptCount: 5, delay: TimeSpan.FromMilliseconds(milliseconds));

            result.Should().NotBeNull();
            result.Should().Be("Task result.");
        }

        private Task<string> CreateTask()
        {
            return Task.FromResult("Task result.");
        }
    }
}
