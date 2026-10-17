class Csshx < Formula
  desc "Cluster ssh tool for Terminal.app"
  homepage "https://github.com/brockgr/csshx"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/csshx/csshX-0.74.tgz"
  mirror "https://distfiles.macports.org/csshX/csshX-0.74.tgz"
  sha256 "eaa9e52727c8b28dedc87398ed33ffa2340d6d0f3ea9d261749c715cb7a0e9c8"
  # same terms as Perl
  license any_of: ["Artistic-1.0-Perl", "GPL-1.0-or-later"]
  head "https://github.com/brockgr/csshx.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/csshx-0.74"
    sha256 cellar: :any, x86_64_windows: "8edb3a7c4c28bdbfd7b3a1c12ac26015890b42f2dcd8be6aaf88d417eba8dd20"
  end

  def install
    bin.install "csshX"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/csshX --version 2>&1", 2)
  end
end
