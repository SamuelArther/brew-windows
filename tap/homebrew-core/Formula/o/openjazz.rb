class Openjazz < Formula
  desc "Open source Jazz Jackrabit engine"
  homepage "https://www.alister.eu/jazz/oj/"
  url "https://github.com/AlisterT/openjazz/releases/download/20260301/openjazz-windows-mingw-x86_64.zip"
  version "20260301"
  sha256 "554761696456a20c1bfc5ebe43bad8537a439d5a00150e4d0bcf9ebcba64a2f5"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/AlisterT/openjazz/archive/refs/tags/20260301.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/OpenJazz.exe", "OpenJazz"
  end

  test do
    assert_path_exists bin/"OpenJazz.exe"
  end
end
