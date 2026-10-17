class Pipebench < Formula
  desc "Measure the speed of STDIN/STDOUT communication"
  homepage "https://www.habets.pp.se/synscan/programs_pipebench.html"
  # Upstream server behaves oddly: https://github.com/Homebrew/homebrew/issues/40897
  # url "http://www.habets.pp.se/synscan/files/pipebench-0.40.tar.gz"
  url "https://deb.debian.org/debian/pool/main/p/pipebench/pipebench_0.40.orig.tar.gz"
  sha256 "ca764003446222ad9dbd33bbc7d94cdb96fa72608705299b6cc8734cd3562211"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?pipebench[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d98822e5c5799dbb3956d2e1125cd9f346100ef0374d3f56c1a3394daa4e1694"
  end

  def install
    # Contacted the upstream author at https://www.habets.pp.se/synscan/contact.html on 2023-09-28
    inreplace "pipebench.c",
              "#include <stdio.h>\n",
              "#include <stdio.h>\n#include <stdlib.h>\n#include <string.h>\n"

    system "make"
    bin.install "pipebench"
    man1.install "pipebench.1"
  end

  test do
    system bin/"pipebench", "-h"
  end
end
