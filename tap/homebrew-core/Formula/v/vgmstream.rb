class Vgmstream < Formula
  desc "Library for playing streamed audio formats from video games"
  homepage "https://vgmstream.org"
  url "https://github.com/vgmstream/vgmstream/releases/download/r2117/vgmstream-win64.zip"
  version "r2117"
  sha256 "6c4a8a3813864fefed081bbd337dbc0ad93bf88e0b92f5db98d7ab258b22dc6c"
  license "ISC"
  revision 1
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vgmstream/vgmstream.git"
    regex(/([^"' >]+)/i)
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/vgmstream-cli.exe", "vgmstream-cli"
  end

  test do
    assert_match "decode", shell_output("#{bin}/vgmstream-cli 2>&1", 1)
  end
end
