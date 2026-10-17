class Cppp < Formula
  desc "Partial Preprocessor for C"
  homepage "https://www.muppetlabs.com/~breadbox/software/cppp.html"
  url "https://www.muppetlabs.com/~breadbox/pub/software/cppp-2.9.tar.gz"
  sha256 "76a95b46c3e36d55c0a98175c0aa72b17b219e68062c2c2c26f971e749951c07"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?cppp[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cppp-2.9"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c18b58c59de4457f8063687c166b22639fa91bb190fb0d1aeee8ced13200d36c"
  end

  def install
    system "make"
    bin.install "cppp"
  end

  test do
    (testpath/"hello.c").write <<~C
      /* Comments stand for code */
      #ifdef FOO
      /* FOO is defined */
      # ifdef BAR
      /* FOO & BAR are defined */
      # else
      /* BAR is not defined */
      # endif
      #else
      /* FOO is not defined */
      # ifndef BAZ
      /* FOO & BAZ are undefined */
      # endif
      #endif
    C
    system bin/"cppp", "-DFOO", "hello.c"
  end
end
