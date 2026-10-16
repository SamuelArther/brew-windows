class Svls < Formula
  desc "SystemVerilog language server"
  homepage "https://github.com/dalance/svls"
  url "https://github.com/dalance/svls/archive/refs/tags/v0.2.14.tar.gz"
  sha256 "d491620c8c89d29a277625afa2e99767f00e7c61cc96e6b91fae709ee1d45ceb"
  license "MIT"
  head "https://github.com/dalance/svls.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1319dcbb00dc3092a0ca1dea3ddfea3332a2b7f1e9959267efa69f356eb34213"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    output = /^Content-Length: \d+\s*$/
    assert_match output, pipe_output(bin/"svls", "\r\n")
  end
end
