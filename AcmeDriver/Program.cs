﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcmeDriver.CLI;
using AcmeDriver.JWK;
using Newtonsoft.Json;

namespace AcmeDriver {
    public class Program {

        private static AcmeClient _client = new AcmeClient(AcmeClient.LETS_ENCRYPT_PRODUCTION_URL);

        public static async Task Main(string[] args) {
            var options = CommandLineOptions.Parse(args);
            await _client.NewNonceAsync();
            Console.WriteLine("AcmeDriver... ready");

            try {
                switch (options.Action) {
                    case "ensure-reg":
                        await EnsureRegistrationAsync(options);
                        break;
                    case "new-reg":
                        await NewRegistrationAsync(options);
                        break;
                    case "dump-reg":
                        var dumpReg = await LoadRegistrationAsync(options.AccountFile);
                        ShowRegistrationInfo(dumpReg);
                        break;
                    case "accept-tos":
                        await LoadRegistrationAsync(options.AccountFile);
                        await _client.AcceptRegistrationAgreementAsync("https://letsencrypt.org/documents/LE-SA-v1.2-November-15-2017.pdf");
                        break;
                    case "new-order":
                        await NewOrderAsync(options);
                        break;
                    case "create-http-authz-files":
                        await CreateHttpAuthzFiles(options);
                        break;
                    case "complete-http-authz-files":
                        await CompleteHttpAuthzFiles(options);
                        break;
                    case "finalize-order":
						await FinalizeOrderAsync(options);
                        break;
                    case "help":
                        Console.WriteLine("help                       Show this screen");
                        Console.WriteLine("new-reg [contacts]+        New registration");
                        Console.WriteLine("load-reg [filename]        Load registration from file");
                        Console.WriteLine("save-reg [filename]        Save registration to file");
                        Console.WriteLine("reg                        Show registration info");
                        Console.WriteLine("new-order [identifier]+    Request new order");
                        Console.WriteLine("order                      Refresh order & show order info");
                        Console.WriteLine("finalize-order [csr-path]  Finalize order");
                        Console.WriteLine("accept-tos                 Accept terms of use");
                        Console.WriteLine("new-authz [domain]         Request new authorization");
                        Console.WriteLine("load-authz [domain]        Load authorization");
                        Console.WriteLine("authz                      Refresh authz & show authorization info");
                        Console.WriteLine("complete-dns-01            Complete dns-01 challenge");
                        Console.WriteLine("complete-http-01           Complete http-01 challenge");
                        Console.WriteLine("prevalidate-dns-01         Prevalidate dns-01 challenge");
                        Console.WriteLine("prevalidate-http-01        Prevalidate http-01 challenge");
                        Console.WriteLine("exit                       Exit");
                        break;
                    case "exit":
                        return;
                    default:
                        Console.WriteLine("unknown command");
                        Console.WriteLine("type help to see help screen");
                        break;
                }
            } catch (Exception exc) {
                WriteErrorLine(exc.Message);
                Console.Write(exc.StackTrace);
            }
        }

        private static async Task EnsureRegistrationAsync(CommandLineOptions options) {
            try {
                await LoadRegistrationAsync(options.AccountFile);
            } catch {
                await NewRegistrationAsync(options);
            }
        }

        private static async Task NewRegistrationAsync(CommandLineOptions options) {
            await _client.NewRegistrationAsync(options.Contacts.ToArray());
            ShowRegistrationInfo(_client.Registration);
            await SaveRegistrationAsync(_client.Registration, options.AccountFile);
        }

        private static async Task NewOrderAsync(CommandLineOptions options) {
            if (options.Domains.Count == 0) {
                ShowNewOrderHelp();
            } else {
                await LoadRegistrationAsync(options.AccountFile);
                var now = DateTime.UtcNow;
                var order = await _client.NewOrderAsync(new AcmeOrder {
                    Identifiers = options.Domains.Select(arg => new AcmeIdentifier { Type = "dns", Value = arg }).ToArray(),
                });
                await SaveOrderAsync(order, options.OrderFile);
                await ShowOrderInfoAsync(order);
            }
        }

		private static async Task FinalizeOrderAsync(CommandLineOptions options) {
			await LoadRegistrationAsync(options.AccountFile);
			var order = await LoadOrderAsync(options.OrderFile);
			var csr = await GetCsrAsync(options.CsrFile);
			await _client.FinalizeOrderAsync(order, csr);
		}
		
		private static async Task CreateHttpAuthzFiles(CommandLineOptions options) {
            await LoadRegistrationAsync(options.AccountFile);
            var order = await LoadOrderAsync(options.OrderFile);
            foreach (var authUri in order.Authorizations) {
                var authz = await _client.GetAuthorizationAsync(new Uri(authUri));
                var httpChallenge = authz.GetHttp01Challenge(_client.Registration);

                var path = Path.Combine(options.ChallengePath, httpChallenge.FileName);
                using var writer = new StreamWriter(path);
                await writer.WriteAsync(httpChallenge.FileContent);
                await writer.FlushAsync();
            }
        }

        private static async Task CompleteHttpAuthzFiles(CommandLineOptions options) {
            await LoadRegistrationAsync(options.AccountFile);
            var order = await LoadOrderAsync(options.OrderFile);

            foreach (var authUri in order.Authorizations) {
                var authz = await _client.GetAuthorizationAsync(new Uri(authUri));
                if (authz.Status == AcmeAuthorizationStatus.Pending) {
                    var httpChallenge = authz.GetHttp01Challenge(_client.Registration);
                    if (httpChallenge != null) {
                        if (await httpChallenge.PrevalidateAsync()) {
                            await _client.CompleteChallengeAsync(httpChallenge);
                        }
                    }
                }
            }
        }

        private static void ShowRegistrationInfo(AcmeClientRegistration reg) {
            Console.WriteLine($"Id:       {reg.Id}");
            Console.WriteLine($"Location: {reg.Location}");
            Console.WriteLine($"JWK:      {reg.GetJwkThumbprint()}");
        }

        private static async Task ShowOrderInfoAsync(AcmeOrder order) {
            Console.WriteLine($"Location: {order.Location}");
            Console.WriteLine($"Status:        {order.Status}");
            Console.WriteLine($"Expires:       {order.Expires:dd MMM yyy}");
            Console.WriteLine($"Identifiers:   {string.Join(", ", order.Identifiers.Select(item => item.ToString()).ToArray())}");

            Console.WriteLine();
            Console.WriteLine("Authorizations:");
            foreach (var authUri in order.Authorizations) {
                var authz = await _client.GetAuthorizationAsync(new Uri(authUri));
                ShowChallengeInfo(authz);
            }
        }

        private static void ShowChallengeInfo(AcmeAuthorization authz) {
            Console.WriteLine($"Authorization: {authz.Identifier.Value}");
            Console.WriteLine($"Status:        {authz.Status}");
            Console.WriteLine($"Expires:       {authz.Expires:dd MMM yyy}");
            Console.WriteLine($"Wildcard:      {authz.Wildcard}");

            Console.WriteLine();
            Console.WriteLine("Challenges:");
            var httpChallenge = authz.GetHttp01Challenge(_client.Registration);
            if (httpChallenge != null) {
                ShowChallengeInfo(httpChallenge);
            }

            var dnsChallenge = authz.GetDns01Challenge(_client.Registration);
            if (dnsChallenge != null) {
                ShowChallengeInfo(dnsChallenge);
            }
        }

        private static void ShowChallengeInfo(AcmeHttp01Challenge httpChallenge) {
            Console.WriteLine("http-01");
            Console.WriteLine($"FileName:      {httpChallenge.FileName}");
            Console.WriteLine($"FileDirectory: {httpChallenge.FileDirectory}");
            Console.WriteLine($"FileContent:   {httpChallenge.FileContent}");
            Console.WriteLine($"FileUri:       {httpChallenge.FileUri}");
            Console.WriteLine($"---------------");
            Console.WriteLine($"uri:           {httpChallenge.Data.Uri}");
            Console.WriteLine();
        }

        private static void ShowChallengeInfo(AcmeDns01Challenge dnsChallenge) {
            Console.WriteLine("dns-01");
            Console.WriteLine($"DnsRecord:        {dnsChallenge.DnsRecord}");
            Console.WriteLine($"DnsRecordType:    TXT");
            Console.WriteLine($"DnsRecordContent: {dnsChallenge.DnsRecordContent}");
            Console.WriteLine($"---------------");
            Console.WriteLine($"uri:              {dnsChallenge.Data.Uri}");
            Console.WriteLine($"nslookup:         {dnsChallenge.NslookupCmd}");
            Console.WriteLine($"google dns:       {dnsChallenge.GoogleUiApiUrl}");
            Console.WriteLine();
        }

        private static async Task<string> GetCsrAsync(string path) {
            try {
                using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var reader = new StreamReader(file)) {
                        var content = await reader.ReadToEndAsync();
                        return content;
                    }
                }
            } catch {
                return null;
            }
        }


        #region Load & Saving

        private static async Task<AcmeClientRegistration> LoadRegistrationAsync(string filename) {
            try {
                using (var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var reader = new StreamReader(file)) {
                        var content = await reader.ReadToEndAsync();
                        var model = Deserialize<AcmeRegistrationModel>(content);
                        var res = Convert(model);
                        _client.Registration = res;
                        return res;
                    }
                }
            } catch {
                WriteErrorLine($"Unable to read account file {filename}");
                return null;
            }
        }

        private static async Task SaveRegistrationAsync(AcmeClientRegistration reg, string filename) {
            try {
                var model = Convert(reg);
                var content = Serialize(model);
                using (var file = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                    using (var writer = new StreamWriter(file)) {
                        await writer.WriteAsync(content);
                    }
                }
            } catch {
            }
        }

        private static async Task<AcmeOrder> LoadOrderAsync(string filename) {
            try {
                using (var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var reader = new StreamReader(file)) {
                        var content = await reader.ReadToEndAsync();
                        var model = Deserialize<AcmeOrderModel>(content);
                        return await _client.GetOrderAsync(model.Location);
                    }
                }
            } catch {
                WriteErrorLine($"Unable to read order file {filename}");
                return null;
            }
        }


        private static async Task SaveOrderAsync(AcmeOrder order, string filename) {
            try {
                var model = Convert(order);
                var content = Serialize(model);
                using (var file = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                    using (var writer = new StreamWriter(file)) {
                        await writer.WriteAsync(content);
                    }
                }
            } catch {
            }
        }

        private static AcmeRegistrationModel Convert(AcmeClientRegistration reg) {
            return new AcmeRegistrationModel {
                Id = reg.Id,
                Key = reg.Key,
                Location = reg.Location
            };
        }

        private static AcmeClientRegistration Convert(AcmeRegistrationModel reg) {
            return new AcmeClientRegistration {
                Id = reg.Id,
                Key = reg.Key,
                Location = reg.Location
            };
        }

        private static AcmeOrderModel Convert(AcmeOrder order) {
            return new AcmeOrderModel {
                Authorizations = order.Authorizations,
                Expires = order.Expires,
                Finalize = order.Finalize,
                Identifiers = order.Identifiers,
                Location = order.Location,
                Status = order.Status,
            };
        }

        private static T Deserialize<T>(string content) {
            return JsonConvert.DeserializeObject<T>(content, new PrivateJwkConverter());
        }

        private static string Serialize(AcmeRegistrationModel reg) {
            return JsonConvert.SerializeObject(reg, Formatting.Indented);
        }

        private static string Serialize(AcmeOrderModel order) {
            return JsonConvert.SerializeObject(order, Formatting.Indented);
        }


        #endregion

        #region Help

        private static void ShowNewOrderHelp() {
            Console.WriteLine("new-order requests new order");
            Console.WriteLine("Usage: new-order domain.com");
        }

        #endregion

        private static void WriteErrorLine(string message) {
            var backup = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = backup;
        }
    }
}

/*

.\AcmeDriver.exe new-reg --contact mailto:savchuk.sergey@gmail.com --account me.json
.\AcmeDriver.exe ensure-reg --contact mailto:savchuk.sergey@gmail.com --account me.json
.\AcmeDriver.exe accept-tos --account me.json
.\AcmeDriver.exe new-order --domain domain.com --order domain.json --account me.json
.\AcmeDriver.exe create-http-authz-files --order domain.json --challenge .  --account me.json
.\AcmeDriver.exe complete-http-authz-files --order domain.json --challenge .  --account me.json
openssl
.\AcmeDriver.exe finalize-order --order domain.json --csr csrpath --account me.json

*/