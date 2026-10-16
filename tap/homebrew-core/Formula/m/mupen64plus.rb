class Mupen64plus < Formula
  desc "Cross-platform plugin-based N64 emulator"
  homepage "https://www.mupen64plus.org/"
  url "https://github.com/mupen64plus/mupen64plus-core/releases/download/2.6.0/mupen64plus-bundle-win64-2.6.0.zip"
  sha256 "8292c68d6ffc3428d4181ac09b1368ef2adb2cc568325545aac78cb6c1e66e21"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mupen64plus/mupen64plus-core/releases/download/2.6.0/mupen64plus-bundle-src-2.6.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/mupen64plus-ui-console.exe", "mupen64plus"
  end

  test do
    assert_path_exists bin/"mupen64plus.exe"
  end
end
