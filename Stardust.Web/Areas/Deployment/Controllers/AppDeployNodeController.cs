﻿using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Web;
using Stardust.Data.Deployment;
using Stardust.Web.Services;
using XCode.Membership;

namespace Stardust.Web.Areas.Deployment.Controllers;

[Menu(88)]
[DeploymentArea]
public class AppDeployNodeController : EntityController<AppDeployNode>
{
    static AppDeployNodeController()
    {
        ListFields.RemoveCreateField();
        AddFormFields.RemoveCreateField();

        LogOnChange = true;
    }

    private readonly DeployService _deployService;
    public AppDeployNodeController(DeployService deployService)
    {
        _deployService = deployService;
    }

    protected override IEnumerable<AppDeployNode> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var entity = AppDeployNode.FindById(id);
            if (entity != null) return new List<AppDeployNode> { entity };
        }

        var appId = p["appId"].ToInt(-1);
        var nodeId = p["nodeId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();

        PageSetting.EnableAdd = appId > 0;
        PageSetting.EnableNavbar = false;

        return AppDeployNode.Search(appId, nodeId, enable, p["Q"], p);
    }

    protected override Boolean Valid(AppDeployNode entity, DataObjectMethodType type, Boolean post)
    {
        if (!post) return base.Valid(entity, type, post);

        var node = entity.Node;
        if (node != null) entity.IP = node.IP;

        entity.App?.Fix();

        return base.Valid(entity, type, post);
    }

    /// <summary>执行操作</summary>
    /// <param name="act"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public async Task<ActionResult> Operate(String act, Int32 id)
    {
        var dn = AppDeployNode.FindById(id);
        if (dn == null || dn.Node == null || dn.App == null) return Json(500, $"[{id}]不存在");

        await _deployService.Control(dn, act, UserHost);

        return JsonRefresh($"在节点[{dn.NodeName}]上对应用[{dn.AppName}]执行[{act}]操作", 1);
    }

    /// <summary>批量执行操作</summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public async Task<ActionResult> BatchOperate(String act)
    {
        var ids = SelectKeys.Select(e => e.ToInt()).Where(e => e > 0).Distinct().ToList();
        foreach (var id in ids)
        {
            var dn = AppDeployNode.FindById(id);
            if (dn != null && dn.Enable && dn.Node != null && dn.App != null)
            {
                await _deployService.Control(dn, act, UserHost);
            }

        }

        return JsonRefresh($"批量执行[{act}]操作", 1);
    }
}