class Sampler < Formula
  desc "Tool for shell commands execution, visualization and alerting"
  homepage "https://sampler.dev"
  url "https://github.com/sqshq/sampler/releases/download/v1.1.0/sampler-1.1.0-windows-amd64.exe"
  sha256 "d3a129deff0e7e1161b293b19130ef174469d1331acb5b0bc0e2513be5b3a51b"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sqshq/sampler/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "sampler-1.1.0-windows-amd64.exe" => "sampler.exe"
  end

  test do
    assert_includes "specify config file", shell_output(bin/"sampler")
  end
end
