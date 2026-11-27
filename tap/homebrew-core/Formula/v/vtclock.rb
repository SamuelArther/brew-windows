class Vtclock < Formula
  desc "Text-mode fullscreen digital clock"
  homepage "https://github.com/dse/vtclock"
  url "https://github.com/dse/vtclock/archive/refs/tags/v0.99.1.tar.gz"
  sha256 "72ce681381ade2442542f2d133eee39eaa1de7f75c11102e31182402c2fe6e23"
  license "GPL-2.0-or-later"
  version_scheme 1
  head "https://github.com/dse/vtclock.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3e9228688f0b444b8636e72fa506f0c4cd80090d959ef113d8efa39a9e5b1ab2"
  end

  depends_on "pkgconf" => :build

  uses_from_macos "ncurses"

  deny_network_access!

  def install
    system "make"
    bin.install "vtclock"
  end

  test do
    system bin/"vtclock", "-h"
  end
end
