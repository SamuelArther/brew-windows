class Treemd < Formula
  desc "TUI and CLI dual pane markdown viewer"
  homepage "https://github.com/epistates/treemd"
  url "https://github.com/Epistates/treemd/releases/download/v0.9.1/treemd-x86_64-pc-windows-msvc.exe.zip"
  sha256 "0e9fcb156b06a41d542e8d397dc7ecb32f9c7dd47906d4c82e2591a0ed4be24a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Epistates/treemd/archive/refs/tags/v0.9.1.tar.gz"
  end

  def install
    bin.install "treemd-x86_64-pc-windows-msvc.exe" => "treemd.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/treemd --version 2>&1")
  end
end
