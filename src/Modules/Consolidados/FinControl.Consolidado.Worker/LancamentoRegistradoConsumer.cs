using System.Text;
using System.Text.Json;
using FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;
using FinControl.Infrastructure.Vault;
using FinControl.SharedKernel.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FinControl.Consolidado.Worker;

public sealed class LancamentoRegistradoConsumer(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<LancamentoRegistradoConsumer> logger)
    : BackgroundService
{
    private const string Exchange = "lancamentos.events";
    private const string Queue = "fincontrol.consolidado.lancamento-registrado";
    private const string RoutingKey = "lancamento.criado";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMqUri = configuration[VaultKeys.RabbitMqUri];

        if (string.IsNullOrEmpty(rabbitMqUri))
        {
            logger.LogWarning("RabbitMQ não configurado — LancamentoRegistradoConsumer inativo.");
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            return;
        }

        var delay = TimeSpan.FromSeconds(5);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerLoopAsync(rabbitMqUri, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Conexão RabbitMQ perdida. Reconectando em {DelaySeconds}s...",
                    delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task RunConsumerLoopAsync(string rabbitMqUri, CancellationToken ct)
    {
        var factory = new ConnectionFactory { Uri = new Uri(rabbitMqUri) };
        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);

        // x-dead-letter-exchange deve bater com o valor que o Wolverine (API de Lancamentos)
        // declarou a fila originalmente — sem este argumento o RabbitMQ rejeita com PRECONDITION_FAILED.
        var queueArgs = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "wolverine-dead-letter-queue" };
        await channel.QueueDeclareAsync(Queue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: queueArgs, cancellationToken: ct);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKey, cancellationToken: ct);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => HandleMessageAsync(channel, args, ct);

        await channel.BasicConsumeAsync(Queue, autoAck: false, consumer, ct);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "LancamentoRegistradoConsumer ativo | Queue={Queue} Exchange={Exchange} RoutingKey={RoutingKey}",
                Queue, Exchange, RoutingKey);

        await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken ct)
    {
        LancamentoRegistradoMessage? message = null;
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.Span);
            message = JsonSerializer.Deserialize<LancamentoRegistradoMessage>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha ao deserializar mensagem. DeliveryTag={DeliveryTag}",
                args.DeliveryTag);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        if (message is null)
        {
            logger.LogWarning("Mensagem deserializada como null. DeliveryTag={DeliveryTag}", args.DeliveryTag);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetService<AtualizarSaldoConsolidaoCommandHandler>();
            if (handler is null)
            {
                logger.LogWarning(
                    "AtualizarSaldoConsolidaoCommandHandler não registrado (Redis indisponível). Descartando mensagem Id={Id}.",
                    message.Id);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, CancellationToken.None);
                return;
            }

            await handler.Handle(new AtualizarSaldoConsolidaoCommand(message.Valor), ct);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Erro ao processar LancamentoRegistrado. Id={Id} Valor={Valor} DeliveryTag={DeliveryTag}",
                message.Id, message.Valor, args.DeliveryTag);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
    }
}
