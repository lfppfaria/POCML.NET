using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using MySql.Data.MySqlClient;
using POCMachineLearning.Entity;
using System;
using System.Linq;

namespace POCMachineLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new MLContext();

            var dataLoader = context.Data.CreateDatabaseLoader<InputModel>();

            var connectionString = "Server=localhost;Database=machine_learning;Uid=root;Pwd=password;";

            var query = @"
                SELECT 
                    sell_year as SellYear, 
                    sell_month as SellMonth, 
                    value as Value
                FROM 
                    input_data;";

            var dataSource = new DatabaseSource(MySqlClientFactory.Instance, connectionString, query);

            var dataView = dataLoader.Load(dataSource);

            //var monthlyDataView = context.Data.FilterRowsByColumn(dataView, "SellMonth");

            var dataPoints = context.Data.CreateEnumerable<InputModel>(dataView, false).Count(); //Quantidade de itens que tenho para dar entrada no treinamento e na previsão

            var forecastPipeline = context.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedSells", //Propriedade do objeto de saída que receberá o valor previsto
                inputColumnName: "Value", //Propriedade da fonte de dados que terá seu valor previsto
                windowSize: 12, //Período do ciclo que estamos medindo, como estou medindo vendas por mês do ano, 12
                seriesLength: dataPoints, //Quantidade de entradas que tenho para fazer o previsão
                trainSize: dataPoints, //Quantidade de entradas que tenho para treinar meu modelo
                horizon: 12, //Período que prever, nesse caso os próximos 12 meses
                confidenceLevel: 0.95f, //Nível de confiança na previsão de melhor e pior cenário, quanto mais alto maior o intervalo entre as previsões de melhor e pior cenário, porém, mais confiável
                confidenceLowerBoundColumn: "LowerBoundSells", //Propriedade do objeto de saída que receberá o valor de previsão de pior cenário 
                confidenceUpperBoundColumn: "UpperBoundSells"); //Propriedade do objeto de saída que receberá o valor de previsão de melhor cenário

            var forecaster = forecastPipeline.Fit(dataView);

            Evaluate(dataView, forecaster, context);

            var forecastEngine = forecaster.CreateTimeSeriesEngine<InputModel, OutputModel>(context);

            forecastEngine.CheckPoint(context, "MLModel.zip");

            Forecast(dataView, 12, forecastEngine, context);
        }

        static void Evaluate(IDataView data, ITransformer model, MLContext context)
        {
            var predictions = model.Transform(data);

            var actual = context.Data.CreateEnumerable<InputModel>(data, true).Select(item => item.Value);

            var forecastxxx = context.Data.CreateEnumerable<OutputModel>(predictions, true).Select(item => item.ForecastedSells);

            var forecast = context.Data.CreateEnumerable<OutputModel>(predictions, true).Select(item => item.ForecastedSells.First());

            var metrics = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);

            var MAE = metrics.Average(error => Math.Abs(error)); // Mean Absolute Error

            var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2))); // Root Mean Squared Error

            Console.WriteLine("Evaluation Metrics");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"Mean Absolute Error: {MAE:F3}");
            Console.WriteLine($"Root Mean Squared Error: {RMSE:F3}\n");
        }

        static void Forecast(IDataView data, int horizon, TimeSeriesPredictionEngine<InputModel, OutputModel> forecaster, MLContext context)
        {
            var originalData = context.Data.CreateEnumerable<InputModel>(data, false);

            var forecast = forecaster.Predict();

            var forecastOutput = context
                .Data
                .CreateEnumerable<InputModel>(data, false)
                .Take(horizon)
                .Select((InputModel sell, int index) =>
                {
                    var pastYearsSells = originalData.Where(item => item.SellMonth.Equals(index + 1)).OrderBy(item => item.SellYear);

                    var stringfiedPastYearsSells = pastYearsSells.Select(item => $"\t{item.SellYear} sells: {item.Value}\n");

                    var month = sell.SellMonth;
                    float actualSells = sell.Value;
                    float lowerEstimate = Math.Max(0, forecast.LowerBoundSells[index]);
                    float estimate = forecast.ForecastedSells[index];
                    float upperEstimate = forecast.UpperBoundSells[index];

                    return $"Month: {month}\n" +
                    $"Other Year Sells:\n{string.Concat(stringfiedPastYearsSells)}\n" +
                    $"Lower Estimate: {lowerEstimate}\n" +
                    $"Forecast: {estimate}\n" +
                    $"Upper Estimate: {upperEstimate}\n";
                });

            foreach (var item in forecastOutput)
            {
                Console.WriteLine(item);
            }
        }
    }
}
