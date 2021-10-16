using FluentAssertions;
using WireGuardServerForWindows.Models;
using WireGuardServerForWindows.Properties;
using Xunit;

namespace WireGuardServerForWindows.Tests
{
    public class ServerConfigurationEndpointPropertyTests
    {
        /// <summary>
        /// Test Fixture
        /// </summary>
        public ServerConfigurationEndpointPropertyTests()
        {
            _endpointConfigurationProperty = (_serverConfiguration = new ServerConfiguration()).EndpointProperty;
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "1")]
        [InlineData("1", null)]
        [InlineData("", "")]
        [InlineData(":", "")]
        [InlineData(":1", "1")]
        [InlineData(":a", "a")]
        [InlineData("1:", "")]
        private void ShouldFailGeneralValidation(string value, string port)
        {
            // Have to set the port to match; do this before setting the value.
            _serverConfiguration.ListenPortProperty.Value = port;

            _endpointConfigurationProperty.Value = value;
            string result = _endpointConfigurationProperty.Validation.Validate(_endpointConfigurationProperty);

            // A non-empty validation result indicates failure, which is what we want for this test.
            result.Should().NotBeNullOrEmpty();

            // None of our failures should be due to a port mismatch.
            result.Should().NotBeEquivalentTo(Resources.EndpointPortMismatch);
        }

        [Theory]
        [InlineData("1.1.1.1:51820", "51821")]
        [InlineData("hostname.com:51820", "51821")]
        [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:51820", "51821")]
        private void ShouldFailPortValidation(string value, string port)
        {
            _serverConfiguration.ListenPortProperty.Value = port;

            _endpointConfigurationProperty.Value = value;
            string result = _endpointConfigurationProperty.Validation.Validate(_endpointConfigurationProperty);

            // This time we're looking for a specific validation error
            result.Should().BeEquivalentTo(Resources.EndpointPortMismatch);
        }

        [Theory]
        [InlineData("1.1.1.1:51820", "51820")]
        [InlineData("hostname.com:51820", "51820")]
        [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:51820", "51820")]
        private void ShouldPassValidation(string value, string port)
        {
            // Have to set the port to match; do this before setting the value.
            _serverConfiguration.ListenPortProperty.Value = port;

            _endpointConfigurationProperty.Value = value;
            string result = _endpointConfigurationProperty.Validation.Validate(_endpointConfigurationProperty);

            // An empty validation result indicates successful validation, which is what we want for this test.
            result.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData(":", "")]
        [InlineData(":1", "")]
        [InlineData("1:", "1")]
        [InlineData("1:2", "1")]
        [InlineData("1.1.1.1:51820", "1.1.1.1")]
        [InlineData("hostname.com:51820", "hostname.com")]
        [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:51820", "[2001:0db8:85a3:0000:0000:8a2e:0370:7334]")]
        private void HostGetterShouldWork(string value, string expectedHost)
        {
            _endpointConfigurationProperty.Value = value;
            _endpointConfigurationProperty.Host.Should().BeEquivalentTo(expectedHost);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData(":", "")]
        [InlineData(":1", "1")]
        [InlineData("1:", "")]
        [InlineData("1:2", "2")]
        [InlineData("1.1.1.1:51820", "51820")]
        [InlineData("hostname.com:51820", "51820")]
        [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:51820", "51820")]
        private void PortGetterShouldWork(string value, string expectedPort)
        {
            _endpointConfigurationProperty.Value = value;
            _endpointConfigurationProperty.Port.Should().BeEquivalentTo(expectedPort);
        }

        [Theory]
        [InlineData(null, "1", "1:")]
        [InlineData("", "1", "1:")]
        [InlineData(":", "1", "1:")]
        [InlineData(":1", "2", "2:1")]
        [InlineData("1:", "2", "2:")]
        [InlineData("1:2", "3", "3:2")]
        [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:51820", "3", "3:51820")]
        private void HostModificationShouldSucceed(string oldValue, string newHost, string newValue)
        {
            _endpointConfigurationProperty.Value = oldValue;
            _endpointConfigurationProperty.Host = newHost;
            _endpointConfigurationProperty.Value.Should().BeEquivalentTo(newValue);
        }

        [Theory]
        [InlineData(null, "1", ":1")]
        [InlineData("", "1", ":1")]
        [InlineData(":", "1", ":1")]
        [InlineData(":1", "2", ":2")]
        [InlineData("1:", "2", "1:2")]
        [InlineData("[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:51820", "3", "[2001:0db8:85a3:0000:0000:8a2e:0370:7334]:3")]
        private void PortModificationShouldSucceed(string oldValue, string newPort, string newValue)
        {
            _endpointConfigurationProperty.Value = oldValue;
            _endpointConfigurationProperty.Port = newPort;
            _endpointConfigurationProperty.Value.Should().BeEquivalentTo(newValue);
        }

        private readonly ServerConfiguration _serverConfiguration;
        private readonly EndpointConfigurationProperty _endpointConfigurationProperty;
    }
}
