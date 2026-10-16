class Intermodal < Formula
  desc "Command-line utility for BitTorrent torrent file creation, verification, etc."
  homepage "https://imdl.io"
  url "https://github.com/casey/intermodal/releases/download/v0.1.16/imdl-v0.1.16-x86_64-pc-windows-msvc.zip"
  sha256 "2de80aadb9c558b542feae7ec676e0bbe0c3d3582a571dafc100df1543601581"
  license "CC0-1.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/casey/intermodal/archive/refs/tags/v0.1.16.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/imdl.exe", "imdl"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/imdl --version 2>&1")
  end
end
