class Ttdl < Formula
  desc "Terminal Todo List Manager"
  homepage "https://github.com/VladimirMarkelov/ttdl"
  url "https://github.com/VladimirMarkelov/ttdl/releases/download/v6.2.2/ttdl_6.2.2_win_x64.zip"
  sha256 "39ee0decdac1f9faa5cd3eef1856fa3eb49775e04dedf80288aa74618b3cd65b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/VladimirMarkelov/ttdl/archive/refs/tags/v6.2.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/ttdl.exe", "ttdl"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ttdl --version 2>&1")
  end
end
