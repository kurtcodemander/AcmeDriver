using System;
using System.Linq;
using AcmeDriver.JWK;
using AcmeDriver.Utils;
using NUnit.Framework;

namespace AcmeDriver.Tests {
    [TestFixture]
    public class AcmeJsonTest {

        [Test]
        public void ReadAcmeChallengeDataTest() {
            var content = @"
{
    ""type"": ""1"",
    ""status"": ""abc"",
    ""url"": ""http://google.com"",
    ""token"": ""token"",
    ""error"": 
    {
        ""type"": ""error.type""
    }
}";
            var data = AcmeJson.Deserialize<AcmeChallengeData>(content, AcmeChallengeDataSourceGenerationContext.Default.AcmeChallengeData);
            Assert.AreEqual("1", data.Type);
            Assert.AreEqual("abc", data.Status);
            Assert.AreEqual(new Uri("http://google.com"), data.Url);
            Assert.AreEqual("token", data.Token);
            Assert.AreEqual("error.type", data.Error?.Type);
        }

        [Test]
        public void ReadAcmeExceptionInfoTest() {
            var content = @"
{
    ""type"": ""1"",
    ""status"": 512,
    ""detail"": ""http://google.com""
}";
            var data = AcmeJson.Deserialize<AcmeExceptionInfo>(content, AcmeExceptionInfoSourceGenerationContext.Default.AcmeExceptionInfo);
            Assert.AreEqual("1", data.Type);
            Assert.AreEqual(512, data.Status);
            Assert.AreEqual("http://google.com", data.Detail);
        }

        [Test]
        public void ReadWriteEccPublicJwkTest() {
            var ecc = new EccPublicJwk {
                Curve = "test",
                X = "---x---",
                Y = "---y---"
            };
			//var json = AcmeJson.Serialize((PublicJsonWebKey)ecc);
			//var result = AcmeJson.Deserialize<EccPublicJwk>(json);
			
			var json = AcmeJson.Serialize(ecc, EccPublicJwkSourceGenerationContext.Default.EccPublicJwk);
			var result = AcmeJson.Deserialize<EccPublicJwk>(json, EccPublicJwkSourceGenerationContext.Default.EccPublicJwk);
			Assert.AreEqual(ecc.Curve, result.Curve);
            Assert.AreEqual(ecc.X, result.X, "X");
            Assert.AreEqual(ecc.Y, result.Y, "Y");
        }

		[Test]
		public void ReadAcmeOrderTest() {
			var content = @"
{
    ""status"": ""invalid"",
    ""identifiers"": [{""type"":""ident1"", ""value"": ""value1""}],    
	""finalize"": ""http://example.com/finalize"",
	""certificate"": ""http://example.com/cert"",
	""authorizations"": [""http://example.com/auth1"", ""http://example.com/auth2""]
}";
			var data = AcmeJson.Deserialize<AcmeOrder>(content, AcmeOrderSourceGenerationContext.Default.AcmeOrder);
			
			Assert.AreEqual(AcmeOrderStatus.Invalid, data.Status);
			Assert.AreEqual("ident1", data.Identifiers.First().Type);
			Assert.AreEqual("value1", data.Identifiers.First().Value);
			Assert.AreEqual(new Uri("http://example.com/auth1"), data.Authorizations.First());
			Assert.AreEqual(new Uri("http://example.com/finalize"), data.Finalize);
			Assert.AreEqual(new Uri("http://example.com/cert"), data.Certificate);			
		}
	}
}
