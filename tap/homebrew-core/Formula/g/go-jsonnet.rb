class GoJsonnet < Formula
  desc "Go implementation of configuration language for defining JSON data"
  homepage "https://jsonnet.org/"
  url "https://github.com/google/go-jsonnet/releases/download/v0.22.0/go-jsonnet_0.22.0_windows_amd64.tar.gz"
  sha256 "277b83ddbe836a627dcde0655720aa42b02277fdf3a9cdaebeef2a53e98599b1"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/go-jsonnet/archive/refs/tags/v0.22.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  conflicts_with "jsonnet", because: "both install binaries with the same name"

  def install
    bin.install "jsonnet.exe"
    bin.install "jsonnet-deps.exe"
    bin.install "jsonnet-lint.exe"
    bin.install "jsonnetfmt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jsonnet --version 2>&1")
  end
end
