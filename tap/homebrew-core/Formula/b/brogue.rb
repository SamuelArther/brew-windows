class Brogue < Formula
  desc "Roguelike game"
  homepage "https://sites.google.com/site/broguegame/"
  url "https://github.com/tmewett/BrogueCE/releases/download/v1.15.1/BrogueCE-1.15.1-windows-x86_64.zip"
  sha256 "29dcbae1791156a040c9125d0a28ae97d67a09b22a52266af1c65e2918bede5f"
  license "AGPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tmewett/BrogueCE/archive/refs/tags/v1.15.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/brogue.exe", "brogue"
  end

  test do
    assert_path_exists bin/"brogue.exe"
  end
end
