class Glider < Formula
  desc "Forward proxy with multiple protocols support"
  homepage "https://github.com/nadoo/glider"
  url "https://github.com/nadoo/glider/releases/download/v0.16.4/glider_0.16.4_windows_amd64.zip"
  sha256 "e0c9687f06c8632902f6bd8e57e20fde28b8b047df602edeaa361c3e9f147cdd"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nadoo/glider/archive/refs/tags/v0.16.4.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/glider.exe", "glider"
  end

  test do
    system bin/"glider", "--help"
  end
end
