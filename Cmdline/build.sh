#!/bin/bash
set -x

ILREPACK_VERSION=2.0.11

xbuild /verbosity:minimal /property:win32icon=assets/ckan.ico CKAN-cmdline.sln

cd ../
sh build.sh

chmod a+x ./Core/packages/ILRepack.$ILREPACK_VERSION/tools/ILRepack.exe

mono ./Core/packages/ILRepack.$ILREPACK_VERSION/tools/ILRepack.exe \
	/target:exe \
	/out:../ckan.exe \
	bin/Debug/CmdLine.exe \
	bin/Debug/CKAN-GUI.exe \
	bin/Debug/ChinhDo.Transactions.FileManager.dll \
	bin/Debug/CKAN.dll \
	bin/Debug/CommandLine.dll \
	bin/Debug/ICSharpCode.SharpZipLib.dll \
	bin/Debug/log4net.dll \
	bin/Debug/Newtonsoft.Json.dll \
	bin/Debug/INIFileParser.dll \
        bin/Debug/CurlSharp.dll

