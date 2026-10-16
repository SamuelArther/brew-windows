class LeafProxy < Formula
  desc "Lightweight and fast proxy utility"
  homepage "https://github.com/eycorsican/leaf"
  url "https://github.com/eycorsican/leaf/releases/download/v0.14.2/leaf-x86_64-pc-windows-gnu.zip"
  sha256 "d04705f049946473e117cff25b82b51bf3cbd421f484e041e390dd5ac640b59d"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/eycorsican/leaf/archive/refs/tags/v0.14.2.tar.gz"
  end

  conflicts_with "leaf-markdown-viewer", because: "both install `leaf` binaries"

  def install
    bin.install "leaf-x86_64-pc-windows-gnu.exe" => "leaf.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/leaf --version 2>&1")
  end
end
