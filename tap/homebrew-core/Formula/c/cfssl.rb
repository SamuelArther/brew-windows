# Upstream ships each tool as its own Windows .exe; the extra tools are resources.
class Cfssl < Formula
  desc "CloudFlare's PKI toolkit"
  homepage "https://cfssl.org/"
  url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/cfssl_1.7.1_windows_amd64.exe"
  sha256 "68a47a3b645190c0390f9859e03809a575e857039becce9bcd8fb4181e82291c"
  license "BSD-2-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  resource "cfssljson" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/cfssljson_1.7.1_windows_amd64.exe"
    sha256 "67dfcef4e3b717081ae1afd255cbd4f34c9d557336fc1aeab4ac6f6af9fb277a"
  end

  resource "cfssl-bundle" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/cfssl-bundle_1.7.1_windows_amd64.exe"
    sha256 "551d8e1c68ef5b8d949ac4835a008998af8d93c2f99d7f2b02c0b879907025fd"
  end

  resource "cfssl-certinfo" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/cfssl-certinfo_1.7.1_windows_amd64.exe"
    sha256 "593e6fbb7b236912080c97efd308eb81387cfbf4e00c1fb70b7781c2b0d9db2d"
  end

  resource "cfssl-newkey" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/cfssl-newkey_1.7.1_windows_amd64.exe"
    sha256 "8785dae66174c8ab672880389093655f13795fc173f198e7cb5fdae060137d03"
  end

  resource "cfssl-scan" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/cfssl-scan_1.7.1_windows_amd64.exe"
    sha256 "07e9ae0b0f49e29e5bc68b90d78053693bb891106f3842f4a3d8f10d0629eb27"
  end

  resource "mkbundle" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/mkbundle_1.7.1_windows_amd64.exe"
    sha256 "5556a01e6a3428970148a953f2d55e9740dbc41bb44707cfb6c18d47445fe876"
  end

  resource "multirootca" do
    url "https://github.com/cloudflare/cfssl/releases/download/v1.7.1/multirootca_1.7.1_windows_amd64.exe"
    sha256 "fb0c5f1fefee784bd734d2ea30a91e8b6574b82d53e94180b1ca855077b91ba0"
  end

  def install
    bin.install "cfssl_1.7.1_windows_amd64.exe" => "cfssl.exe"
    install_resource "cfssljson", "libexec/cfssljson"
    windows_exe "libexec/cfssljson/cfssljson_1.7.1_windows_amd64.exe", "cfssljson"
    install_resource "cfssl-bundle", "libexec/cfssl-bundle"
    windows_exe "libexec/cfssl-bundle/cfssl-bundle_1.7.1_windows_amd64.exe", "cfssl-bundle"
    install_resource "cfssl-certinfo", "libexec/cfssl-certinfo"
    windows_exe "libexec/cfssl-certinfo/cfssl-certinfo_1.7.1_windows_amd64.exe", "cfssl-certinfo"
    install_resource "cfssl-newkey", "libexec/cfssl-newkey"
    windows_exe "libexec/cfssl-newkey/cfssl-newkey_1.7.1_windows_amd64.exe", "cfssl-newkey"
    install_resource "cfssl-scan", "libexec/cfssl-scan"
    windows_exe "libexec/cfssl-scan/cfssl-scan_1.7.1_windows_amd64.exe", "cfssl-scan"
    install_resource "mkbundle", "libexec/mkbundle"
    windows_exe "libexec/mkbundle/mkbundle_1.7.1_windows_amd64.exe", "mkbundle"
    install_resource "multirootca", "libexec/multirootca"
    windows_exe "libexec/multirootca/multirootca_1.7.1_windows_amd64.exe", "multirootca"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cfssl version")
  end
end
