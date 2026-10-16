class Legba < Formula
  desc "Multiprotocol credentials bruteforcer/password sprayer and enumerator"
  homepage "https://legba.evilsocket.net/"
  url "https://github.com/evilsocket/legba/releases/download/1.3.0/legba-1.3.0-windows-x86_64.zip"
  sha256 "f9c8737b6110c2cd1fb6ad79df0db9a916fd4bb86f49fdcdedbad9e16960741b"
  license "AGPL-3.0-only"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/evilsocket/legba/archive/refs/tags/1.3.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/legba.exe", "legba"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/legba --version")

    output = shell_output("#{bin}/legba --list-plugins")
    assert_match "Samba password authentication", output
  end
end
