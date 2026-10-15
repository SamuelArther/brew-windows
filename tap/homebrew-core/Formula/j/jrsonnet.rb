class Jrsonnet < Formula
  desc "Rust implementation of Jsonnet language"
  homepage "https://github.com/deltarocks/jrsonnet"
  url "https://github.com/deltarocks/jrsonnet/releases/download/v0.4.2/jrsonnet-windows-msvc-amd64.exe"
  sha256 "dd3fe80bf46c880444cdd2eadfd8c2a889e97fcbfd3cc7d6961155912f47e03f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/deltarocks/jrsonnet/archive/refs/tags/v0.4.2.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "jrsonnet-windows-msvc-amd64.exe" => "jrsonnet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jrsonnet --version 2>&1")
  end
end
