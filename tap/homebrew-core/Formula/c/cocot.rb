class Cocot < Formula
  desc "Code converter on tty"
  homepage "https://vmi.jp/software/cygwin/cocot.html"
  url "https://github.com/vmi/cocot/archive/refs/tags/cocot-1.2-20171118.tar.gz"
  sha256 "b718630ce3ddf79624d7dcb625fc5a17944cbff0b76574d321fb80c61bb91e4c"
  license "BSD-3-Clause"
  head "https://github.com/vmi/cocot.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cocot-1.2-20171118"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3a6ede12dddf4b7ee8b055efce6b57300b2c2422374f5baf803a7189c1029ecc"
  end

  def install
    # Workaround for newer Clang
    ENV.append_to_cflags "-Wno-incompatible-function-pointer-types" if DevelopmentTools.clang_build_version >= 1500

    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cocot --version 2>&1", 1)
  end
end
