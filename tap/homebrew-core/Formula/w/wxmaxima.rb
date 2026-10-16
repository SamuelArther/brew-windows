class Wxmaxima < Formula
  desc "Cross platform GUI for Maxima"
  homepage "https://wxmaxima-developers.github.io/wxmaxima/"
  url "https://github.com/wxMaxima-developers/wxmaxima/releases/download/Version-26.09.0/wxMaxima-26.09.0-win64.zip"
  sha256 "306cba8472da143655cefa653a2ab15061f2770fc3b6121b17d08a09669e5942"
  license "GPL-2.0-or-later"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wxMaxima-developers/wxmaxima/archive/refs/tags/Version-26.09.0.tar.gz"
    regex(/^Version[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/wxmaxima.exe", "wxmaxima"
  end

  test do
    assert_path_exists bin/"wxmaxima.exe"
  end
end
