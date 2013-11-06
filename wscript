#!/usr/bin/env python
# encoding: utf-8
# Copyright (c) 2012 SjB <steve@nca.uwo.ca>. All Rights Reserved.

import os
from waflib import Options

APPNAME = 'Magma.JSON'
VERSION = '2.0'

top = '.'
out = 'build'

waftools_dir = 'waftools/sjb-waftools/extra'
def options(ctx):
    ctx.load('cs csproj', tooldir=waftools_dir)


def configure(ctx):
    ctx.load('cs csproj', tooldir=waftools_dir)

    ctx.env.append_value('PropertyGroup', 'TargetFrameworkVersion=v3.5')

    ctx.env.default_app_install_path = os.path.join('${PREFIX}', 'lib', APPNAME)


def build(bld):
    srcs = ['Properties/AssemblyInfo.cs', 'Magma.JSON.cs']

    bld(
        features = 'cs',
        source = srcs,
        install_path = bld.env.default_app_install_path,
        type = 'library',
        target = 'Magma.JSON.dll',
        name = 'Magma.JSON',
        use = 'System System.Core System.Web System.Xml System.Data')

    bld.install_files(bld.env.default_app_install_path, 'app.config')
