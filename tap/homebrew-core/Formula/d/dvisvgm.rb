class Dvisvgm < Formula
  desc "Fast DVI to SVG converter"
  homepage "https://dvisvgm.de"
  url "https://github.com/mgieseki/dvisvgm/releases/download/3.6.1/dvisvgm-3.6.1-win64.zip"
  sha256 "0fedc0a9511c0e1ae67cd5905a3c2371d3629215fb5aafbfd1dcfaa32f2bdb99"
  license "GPL-3.0-or-later"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mgieseki/dvisvgm/releases/download/3.6.1/dvisvgm-3.6.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/dvisvgm.exe", "dvisvgm"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dvisvgm --version 2>&1")
  end
end
