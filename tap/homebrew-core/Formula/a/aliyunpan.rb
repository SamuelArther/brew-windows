class Aliyunpan < Formula
  desc "Command-line client tool for Alibaba aDrive disk"
  homepage "https://github.com/tickstep/aliyunpan"
  url "https://github.com/tickstep/aliyunpan/releases/download/v0.4.0/aliyunpan-v0.4.0-windows-x64.zip"
  sha256 "471dfc6538f1032114a0b5ba5c69461e15179cb8b29137ea29f158013d4c8759"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tickstep/aliyunpan/archive/refs/tags/v0.4.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/aliyunpan.exe", "aliyunpan"
  end

  test do
    system bin/"aliyunpan", "run", "touch", "output.txt"
    assert_path_exists testpath/"output.txt"
  end
end
