class Reg < Formula
  desc "Docker registry v2 command-line client"
  # original homepage is down `https://r.j3ss.co`
  homepage "https://github.com/genuinetools/reg"
  url "https://github.com/genuinetools/reg/archive/refs/tags/v0.16.1.tar.gz"
  sha256 "b65787bff71bff21f21adc933799e70aa9b868d19b1e64f8fd24ebdc19058430"
  license "MIT"

  deprecate! date: "2026-08-23", because: :unmaintained
  disable! date: "2027-08-23", because: :unmaintained

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "07cfb52d925a91e8fd6a1684bc124bad335b266c4f00331475501ef729aace5e"
  end

  depends_on "go" => :build

  # Failing on Intel macOS with Go 1.27 due to outdated golang.org/x/sys dependency
  on_macos do
    depends_on arch: :arm64
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match "buster", shell_output("#{bin}/reg tags debian")
  end
end
