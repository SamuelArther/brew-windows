class Stella < Formula
  desc "Atari 2600 VCS emulator"
  homepage "https://stella-emu.github.io/"
  url "https://github.com/stella-emu/stella/releases/download/7.0/Stella-7.0c-windows.zip"
  version "7.0c"
  sha256 "44102ccf8af74b021cf143db346e561c5d6ed2a2e91d79ee6d3c99d7c42f4653"
  license "GPL-2.0-or-later"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stella-emu/stella/archive/refs/tags/7.0c.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/Stella.exe", "stella"
  end

  test do
    assert_path_exists bin/"stella.exe"
  end
end
