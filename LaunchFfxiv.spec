Name:           LaunchFfxiv
Version:        0.0.0
Release:        0%{?dist}
Summary:        Launch wrapped for FFXIV

License:        CC-BY-SA3
URL:            https://github.com/joker-119/LaunchFfxiv
Source0:        %{name}-%{version}.tar.gz

BuildRequires: dotnet-sdk-6.0
#Requires:

%description
Launch wrapper that launches XLCore/XIVLauncher, IINACT and RPCAPD for you with optimal settings.

%prep
%autosetup


%build
dotnet publish -r linux-x64 -c Release -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true


%install
mv LaunchFfxiv/bin/Release/net6.0/linux-x64/publish/LaunchFfxiv /opt/LaunchFfxiv
ls /opt

%files
/opt/LaunchFfxiv
%license LICENSE.md


%changelog
* Tue Sep 06 2022 Joker <amathor929@gmail.com>
- 
