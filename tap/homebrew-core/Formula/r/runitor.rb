class Runitor < Formula
  desc "Command runner with healthchecks.io integration"
  homepage "https://github.com/bdd/runitor"
  url "https://github.com/bdd/runitor/releases/download/v1.4.1-build.6/runitor-v1.4.1-build.6-windows-amd64.exe"
  version "1.4.1"
  sha256 "0a17f9db6637c87edb5ef224303d4d806edd876874617e58b9341fde9e928896"
  license "0BSD"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bdd/runitor/archive/refs/tags/v1.4.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "runitor-v1.4.1-build.6-windows-amd64.exe" => "runitor.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/runitor --version 2>&1")
  end
end
