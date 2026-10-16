class LeelaZero < Formula
  desc "Neural Network Go engine with no human-provided knowledge"
  homepage "https://zero.sjeng.org/"
  url "https://github.com/leela-zero/leela-zero/releases/download/v0.17/leela-zero-0.17-cpuonly-win64.zip"
  sha256 "378c2fcc91cb0047bfbe7dd69b415ef27234fa49281bca25ccf9f047178ab161"
  license "GPL-3.0-or-later"
  revision 13

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/leela-zero/leela-zero.git"
  end

  def install
    install_tree
    windows_exe "libexec/leelaz.exe", "leelaz"
  end

  test do
    system bin/"leelaz", "--help"
  end
end
