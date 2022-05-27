﻿using System.Diagnostics;
using NewLife;
using NewLife.Agent;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;
using Stardust;
using Stardust.Managers;
using Stardust.Models;

namespace StarAgent
{
    [Api(null)]
    public class StarService : IApi
    {
        #region 属性
        /// <summary>
        /// 网络会话
        /// </summary>
        public IApiSession Session { get; set; }

        /// <summary>服务对象</summary>
        public ServiceBase Service { get; set; }

        /// <summary>服务主机</summary>
        public IHost Host { get; set; }

        /// <summary>本地应用服务管理</summary>
        public ServiceManager Manager { get; set; }

        /// <summary>星尘设置</summary>
        public StarSetting StarSetting { get; set; }

        /// <summary>星尘代理设置</summary>
        public Setting AgentSetting { get; set; }
        #endregion

        #region 业务
        /// <summary>信息</summary>
        /// <returns></returns>
        [Api(nameof(Info))]
        public AgentInfo Info(AgentInfo info)
        {
            XTrace.WriteLine(info.ToJson());

            var set = StarSetting;
            // 使用对方送过来的星尘服务端地址
            if (set.Server.IsNullOrEmpty() && !info.Server.IsNullOrEmpty())
            {
                set.Server = info.Server;
                set.Save();

                XTrace.WriteLine("StarAgent使用应用[{0}]送过来的星尘服务端地址：{1}", info.ProcessName, info.Server);

                if (Service is MyService svc)
                {
                    ThreadPoolX.QueueUserWorkItem(() =>
                    {
                        svc.StartFactory();
                        svc.StartClient();
                    });
                }
            }

            var ai = AgentInfo.GetLocal();
            ai.Server = set.Server;
            ai.Services = Manager?.Services.Select(e => e.Name).ToArray();
            ai.Code = AgentSetting.Code;

            return ai;
        }

        /// <summary>杀死并启动进程</summary>
        /// <param name="processId">进程</param>
        /// <param name="delay">延迟结束的秒数</param>
        /// <param name="fileName">文件名</param>
        /// <param name="arguments">参数</param>
        /// <param name="workingDirectory">工作目录</param>
        /// <returns></returns>
        [Api(nameof(KillAndStart))]
        public Object KillAndStart(Int32 processId, Int32 delay, String fileName, String arguments, String workingDirectory)
        {
            if (Session is INetSession ns && !ns.Remote.Address.IsLocal()) throw new InvalidOperationException("禁止非本机操作！");

            var p = Process.GetProcessById(processId);
            if (p == null) throw new InvalidOperationException($"无效进程Id[{processId}]");

            var name = p.ProcessName;
            var pid = 0;

            ThreadPoolX.QueueUserWorkItem(() =>
            {
                WriteLog("杀死进程 {0}/{1}，等待 {2}秒", processId, p.ProcessName, delay);

                if (delay > 0) Thread.Sleep(delay * 1000);

                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                        p.WaitForExit(5_000);
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }

                // 启动进程
                if (!fileName.IsNullOrEmpty())
                {
                    WriteLog("启动进程：{0} {1} {2}", fileName, arguments, workingDirectory);

                    var si = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,

                        // false时目前控制台合并到当前控制台，一起退出；
                        // true时目标控制台独立窗口，不会一起退出；
                        UseShellExecute = true,
                    };

                    var p2 = Process.Start(si);
                    pid = p2.Id;

                    WriteLog("应用[{0}]启动成功 PID={1}", p2.ProcessName, p2.Id);
                }
            });

            return new { name, pid };
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}