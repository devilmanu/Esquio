﻿using Esquio;
using Esquio.Abstractions;
using Esquio.AspNetCore.Toggles;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using UnitTests.Builders;
using UnitTests.Seedwork;
using Xunit;

namespace UnitTests.Esquio.AspNetCore.Toggles
{
    public class gradualrolloutsession_should
    {
        [Fact]
        public void throw_if_partitioner_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var accessor = new FakeHttpContextAccessor();

                new GradualRolloutSessionToggle(null, accessor);
            });
        }

        [Fact]
        public void throw_if_httpcontextaccessor_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var partitioner = new DefaultValuePartitioner();

                new GradualRolloutSessionToggle(partitioner, null);
            });
        }

        [Fact]
        public async Task be_active_when_session_is_on_valid_partition()
        {
            var toggle = Build
                .Toggle<GradualRolloutSessionToggle>()
                .AddParameter("Percentage", 100)
                .Build();

            var feature = Build
                .Feature(Constants.FeatureName)
                .AddOne(toggle)
                .Build();

            var context = new DefaultHttpContext();
            context.Session = new FakeSession();

            var partitioner = new DefaultValuePartitioner();

            var gradualRolloutSession = new GradualRolloutSessionToggle(partitioner, new FakeHttpContextAccessor(context));

            var active = await gradualRolloutSession.IsActiveAsync(
                ToggleExecutionContext.FromToggle(
                    feature.Name,
                    EsquioConstants.DEFAULT_PRODUCT_NAME,
                    EsquioConstants.DEFAULT_DEPLOYMENT_NAME,
                    toggle));

            active.Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(40)]
        [InlineData(10)]
        [InlineData(70)]
        [InlineData(72)]
        [InlineData(30)]
        [InlineData(80)]
        [InlineData(100)]
        [InlineData(11)]
        [InlineData(33)]
        public async Task use_partition_for_session(int percentage)
        {
            var valid = false;
            var sessionId = string.Empty;

            do
            {
                sessionId = Guid.NewGuid().ToString();
                var partition = new DefaultValuePartitioner().ResolvePartition(Constants.FeatureName + sessionId, partitions: 100);

                if (partition <= percentage)
                {
                    valid = true;
                }

            } while (!valid);

            var toggle = Build
               .Toggle<GradualRolloutSessionToggle>()
               .AddParameter("Percentage", percentage)
               .Build();

            var feature = Build
                .Feature(Constants.FeatureName)
                .AddOne(toggle)
                .Build();

            var context = new DefaultHttpContext();
            context.Session = new FakeSession(sessionId);

            var partitioner = new DefaultValuePartitioner();

            var gradualRolloutSession = new GradualRolloutSessionToggle(partitioner, new FakeHttpContextAccessor(context));

            var active = await gradualRolloutSession.IsActiveAsync(
                ToggleExecutionContext.FromToggle(
                    feature.Name,
                    EsquioConstants.DEFAULT_PRODUCT_NAME,
                    EsquioConstants.DEFAULT_DEPLOYMENT_NAME,
                    toggle));

            active.Should()
                .BeTrue();
        }

        [Fact]
        public async Task be_non_active_when_claim_value_is_not_on_valid_partition()
        {
            var toggle = Build
                .Toggle<GradualRolloutSessionToggle>()
                .AddParameter("Percentage", 0)
                .Build();

            var feature = Build
                .Feature(Constants.FeatureName)
                .AddOne(toggle)
                .Build();

            var context = new DefaultHttpContext();
            context.Session = new FakeSession();

            var partitioner = new DefaultValuePartitioner();

            var gradualRolloutSession = new GradualRolloutSessionToggle(partitioner, new FakeHttpContextAccessor(context));

            var active = await gradualRolloutSession.IsActiveAsync(
                ToggleExecutionContext.FromToggle(
                    feature.Name,
                    EsquioConstants.DEFAULT_PRODUCT_NAME,
                    EsquioConstants.DEFAULT_DEPLOYMENT_NAME,
                    toggle));

            active.Should()
                .BeFalse();
        }

        [Fact]
        public async Task throw_when_session_is_not_active()
        {
            var toggle = Build
                .Toggle<GradualRolloutSessionToggle>()
                .AddParameter("Percentage", 100)
                .Build();

            var feature = Build
                .Feature(Constants.FeatureName)
                .AddOne(toggle)
                .Build();

            var context = new DefaultHttpContext();

            var partitioner = new DefaultValuePartitioner();

            var gradualRolloutSession = new GradualRolloutSessionToggle(partitioner, new FakeHttpContextAccessor(context));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await gradualRolloutSession.IsActiveAsync(
                    ToggleExecutionContext.FromToggle(
                    feature.Name,
                    EsquioConstants.DEFAULT_PRODUCT_NAME,
                    EsquioConstants.DEFAULT_DEPLOYMENT_NAME,
                    toggle));
            });
        }

    }
}
