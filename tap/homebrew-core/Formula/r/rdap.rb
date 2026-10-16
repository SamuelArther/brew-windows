class Rdap < Formula
  desc "Command-line client for the Registration Data Access Protocol"
  homepage "https://www.openrdap.org"
  url "https://github.com/openrdap/rdap/releases/download/v0.10.2/rdap-windows-amd64.exe.zip"
  sha256 "a00ce41ad8607e077cf65741d5bb1e43d1e40105e307f5ed9d7caf238668550a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/openrdap/rdap/archive/refs/tags/v0.10.2.tar.gz"
  end

  def install
    bin.install "rdap-windows-amd64.exe" => "rdap.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rdap --version 2>&1")
  end
end
