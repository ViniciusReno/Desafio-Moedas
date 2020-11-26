using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using worker.Models;
using worker.Util;

namespace worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfigurations _serviceConfigurations;

        public Worker(ILogger<Worker> logger,
            IConfiguration configuration)
        {
            _logger = logger;

            _serviceConfigurations = new ServiceConfigurations();
            new ConfigureFromConfigurationOptions<ServiceConfigurations>(
                configuration.GetSection("ServiceConfigurations"))
                    .Configure(_serviceConfigurations);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker executando em: {time}", DateTimeOffset.Now);

                foreach (string host in _serviceConfigurations.Hosts)
                {
                    _logger.LogInformation($"Verificando itens em {host}");
                    var Status = new Status();

                    var client = new RestClient(host);
                    var request = new RestRequest(Method.GET);
                    IRestResponse response = await client.ExecuteAsync(request);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }
                    var result = JsonConvert.DeserializeObject<ItemFila>(response.Content);
                    if (result.Id > 0)
                    {
                        try
                        {
                            List<ResultadoBusca> resultadosBusca = new List<ResultadoBusca>();
                            List<DadosMoeda> dadosMoedas = ReadDadosMoeda();
                            foreach (var item in dadosMoedas)
                            {
                                if (item.ID_MOEDA == result.Moeda && Between(item.DATA_REF, result.DataInicio, result.DataFim))
                                {
                                    resultadosBusca.Add(new ResultadoBusca
                                    {
                                        ID_MOEDA = item.ID_MOEDA,
                                        DATA_REF = item.DATA_REF,
                                    });
                                }
                            }

                            var dadosCotacao = ReadDadosCotacao();
                            var idCotacao = (int)((DePara)Enum.Parse(typeof(DePara), result.Moeda));
                            foreach (var item in dadosCotacao)
                            {
                                foreach (var busca in resultadosBusca)
                                {
                                    if (item.cod_cotacao == idCotacao.ToString() &&
                                        DateTime.Parse(busca.DATA_REF) == DateTime.Parse(item.dat_cotacao))
                                    {
                                        busca.VL_COTACAO = item.vlr_cotacao;
                                    }
                                }
                            }
                            WriteFile(resultadosBusca);

                            // criar csv com o resultadosBusca
                            Status.Msg = "Sucesso";
                        }
                        catch (Exception ex)
                        {
                            Status.Msg = "Exception";
                            Status.Exception = ex.Message;
                        }
                    }

                    Status.FinalizadoEm = DateTime.Now.ToString("G");
                    string jsonResultado = JsonConvert.SerializeObject(Status);
                    if (Status.Exception == null)
                        _logger.LogInformation(jsonResultado);
                    else
                        _logger.LogError(jsonResultado);
                }

                await Task.Delay(
                    _serviceConfigurations.Intervalo, stoppingToken);
            }
        }

        private static List<DadosMoeda> ReadDadosMoeda()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "DadosMoeda.csv");
            var reader = new StreamReader(path);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Configuration.Delimiter = ";";
            var records = csv.GetRecords<DadosMoeda>();
            return records.ToList();
        }

        private static List<DadosCotacao> ReadDadosCotacao()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "DadosCotacao.csv");
            var reader = new StreamReader(path);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Configuration.Delimiter = ";";
            var records = csv.GetRecords<DadosCotacao>();
            return records.ToList();
        }

        private static bool Between(string input, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                var value = DateTime.Parse(input);
                return value > dataInicio && value < dataFim;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void WriteFile(List<ResultadoBusca> data)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string finalPath = Path.Combine(basePath, "Resultado_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                string header = "";
                var info = typeof(ResultadoBusca).GetProperties();
                if (!File.Exists(finalPath))
                {
                    var file = File.Create(finalPath);
                    file.Close();
                    foreach (var prop in typeof(ResultadoBusca).GetProperties())
                    {
                        header += prop.Name + "; ";
                    }
                    header = header.Substring(0, header.Length - 2);
                    sb.AppendLine(header);
                    TextWriter sw = new StreamWriter(finalPath, true);
                    sw.Write(sb.ToString());
                    sw.Close();
                }
                foreach (var obj in data)
                {
                    sb = new StringBuilder();
                    var line = "";
                    foreach (var prop in info)
                    {
                        line += prop.GetValue(obj, null) + "; ";
                    }
                    line = line.Substring(0, line.Length - 2);
                    sb.AppendLine(line);
                    TextWriter sw = new StreamWriter(finalPath, true);
                    sw.Write(sb.ToString());
                    sw.Close();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
    }
}