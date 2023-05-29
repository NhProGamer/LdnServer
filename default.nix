{ lib, stdenv, fetchNuGet, buildDotnetModule, dotnetCorePackages }:

let
in

buildDotnetModule rec {
  pname = "ryujinx-ldn-server";
  version = "0.1";

  src = ./.;

  projectFile = "LanPlayServer.sln";
  nugetDeps = ./deps.nix;

  dotnet-sdk = dotnetCorePackages.sdk_7_0;
  dotnet-runtime = dotnetCorePackages.runtime_7_0;
  selfContainedBuild = false;

  dotnetFlags = [  ];

  executables = [ "LanPlayServer" ];
}