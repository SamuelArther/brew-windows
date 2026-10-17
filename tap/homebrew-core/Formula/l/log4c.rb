class Log4c < Formula
  desc "Logging Framework for C"
  homepage "https://log4c.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/log4c/log4c/1.2.4/log4c-1.2.4.tar.gz"
  sha256 "5991020192f52cc40fa852fbf6bbf5bd5db5d5d00aa9905c67f6f0eadeed48ea"
  license "LGPL-2.1-only"
  head "https://git.code.sf.net/p/log4c/log4c.git", branch: "master"

  livecheck do
    url :stable
    regex(%r{url=.*?/log4c[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "1c7274c751b017e4f4bf65d6258c7420b7c49a236e98485f47ec40ff587f4bcb"
  end

  # Fix -flat_namespace being used on Big Sur and later.
  patch do
    file "Patches/libtool/configure-pre-0.4.2.418-big_sur.diff"
  end

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"log4c-config", "--version"
  end
end
