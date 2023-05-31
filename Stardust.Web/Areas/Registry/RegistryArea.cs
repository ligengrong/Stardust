﻿using System;
using System.ComponentModel;
using NewLife;
using NewLife.Cube;

namespace Stardust.Web.Areas.Registry;

[DisplayName("注册中心")]
[Menu(777, true)]
public class RegistryArea : AreaBase
{
    public RegistryArea() : base(nameof(RegistryArea).TrimEnd("Area")) { }

    static RegistryArea() => RegisterArea<RegistryArea>();
}