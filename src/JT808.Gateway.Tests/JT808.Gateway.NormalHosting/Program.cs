﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JT808.Protocol;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using JT808.Gateway.NormalHosting.Impl;
using JT808.Gateway.MsgLogging;
using JT808.Gateway.Transmit;
using JT808.Gateway.Traffic;
using JT808.Gateway.NormalHosting.Services;
using JT808.Gateway.Abstractions;
using JT808.Gateway.SessionNotice;
using JT808.Gateway.Client;
using JT808.Gateway.NormalHosting.Jobs;

namespace JT808.Gateway.NormalHosting
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serverHostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{ hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    Console.WriteLine($"Environment.OSVersion.Platform:{Environment.OSVersion.Platform.ToString()}");
                    NLog.LogManager.LoadConfiguration($"Configs/nlog.{Environment.OSVersion.Platform.ToString()}.config");
                    logging.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    //使用内存队列实现会话通知
                    services.AddSingleton<JT808SessionService>();
                    services.AddSingleton<IJT808SessionProducer, JT808SessionProducer>();
                    services.AddSingleton<IJT808SessionConsumer, JT808SessionConsumer>();
                    services.AddJT808Configure()
                            //添加客户端工具
                            //.AddClient()
                            //.AddNormalGateway(options =>
                            ////{
                            ////    options.TcpPort = 808;
                            ////    options.UdpPort = 808;
                            ////})                            
                            .AddNormalGateway(hostContext.Configuration)
                            .ReplaceNormalReplyMessageHandler<JT808NormalReplyMessageHandlerImpl>()
                            .AddMsgLogging<JT808MsgLogging>()
                            .AddTraffic()
                            .AddSessionNotice()
                            .AddTransmit(hostContext.Configuration)
                            .AddTcp()
                            .AddUdp()
                            .AddGrpc()
                            ;
                    //流量统计
                    services.AddHostedService<TrafficJob>();
                    //grpc客户端调用
                    //services.AddHostedService<CallGrpcClientJob>();
                    //客户端测试
                    //services.AddHostedService<UpJob>();
                });

            await serverHostBuilder.RunConsoleAsync();
        }
    }
}
